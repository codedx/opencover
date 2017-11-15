#include "stdafx.h"

#include "AssemblyRegistry.h"
#include "InjectedType.h"
#include "InjectedTypeTestFixture.h"
#include "MockIMethodMalloc.h"

using namespace Injection;
using namespace Instrumentation;
using namespace testing;
using namespace std;

class InjectedTypeTest : public InjectedTypeTestFixture 
{
protected:
	InjectedTypeTest() :
		InjectedTypeTestFixture()
	{
	}

	void SetUp() override
	{
	}

	void TearDown() override
	{
	}
};

class MockInjectedType : public InjectedType
{
public:
	MockInjectedType(const CComPtr<ICorProfilerInfo>& profilerInfo,
		const shared_ptr<AssemblyRegistry>& assemblyRegistry) :
		InjectedType(profilerInfo, assemblyRegistry)
	{
	}

protected:
	MOCK_CONST_METHOD1(ShouldRegisterType, bool(const ModuleID moduleId));
	MOCK_METHOD1(RegisterType, HRESULT(const ModuleID moduleId));
	MOCK_METHOD1(InjectTypeImplementation, HRESULT(const ModuleID moduleId));

	FRIEND_TEST(InjectedTypeTest, DoesNotCallRegisterTypeIfTypeShouldNotBeRegisteredInModule);
	FRIEND_TEST(InjectedTypeTest, CallsRegisterTypeIfTypeShouldBeRegisteredInModule);
	FRIEND_TEST(InjectedTypeTest, FailsToInjectImplementationIfRegisterTypeNotCalledForModule);
	FRIEND_TEST(InjectedTypeTest, FailsToInjectImplementationIfRegisterTypeCalledForOtherModule);
	FRIEND_TEST(InjectedTypeTest, CanReplaceBasicMethod);
	FRIEND_TEST(InjectedTypeTest, CanReplaceMethodWithLocalVariablesAndCustomStackSize);
	FRIEND_TEST(InjectedTypeTest, CanReplaceMethodWithExceptionHandler);
};

TEST_F(InjectedTypeTest, DoesNotCallRegisterTypeIfTypeShouldNotBeRegisteredInModule)
{
	MockInjectedType injectedType(profilerInfoPtr, assemblyRegistry);

	EXPECT_CALL(injectedType, ShouldRegisterType(_))
		.WillOnce(Return(false));

	EXPECT_CALL(injectedType, RegisterType(_))
		.Times(0);

	injectedType.RegisterTypeInModule(1);
}

TEST_F(InjectedTypeTest, CallsRegisterTypeIfTypeShouldBeRegisteredInModule)
{
	MockInjectedType injectedType(profilerInfoPtr, assemblyRegistry);

	EXPECT_CALL(injectedType, ShouldRegisterType(_))
		.WillOnce(Return(true));

	EXPECT_CALL(injectedType, RegisterType(_))
		.Times(1);

	injectedType.RegisterTypeInModule(1);
}

TEST_F(InjectedTypeTest, FailsToInjectImplementationIfRegisterTypeNotCalledForModule)
{
	MockInjectedType injectedType(profilerInfoPtr, assemblyRegistry);

	ASSERT_EQ(E_ILLEGAL_METHOD_CALL, injectedType.InjectTypeImplementationInModule(1));
}

TEST_F(InjectedTypeTest, FailsToInjectImplementationIfRegisterTypeCalledForOtherModule)
{
	MockInjectedType injectedType(profilerInfoPtr, assemblyRegistry);

	EXPECT_CALL(injectedType, ShouldRegisterType(_))
		.WillOnce(Return(true));

	EXPECT_CALL(injectedType, RegisterType(_))
		.WillOnce(Return(S_OK));

	EXPECT_CALL(injectedType, InjectTypeImplementation(_))
		.WillOnce(Return(S_OK));

	injectedType.RegisterTypeInModule(1);

	ASSERT_EQ(E_ILLEGAL_METHOD_CALL, injectedType.InjectTypeImplementationInModule(2));
	ASSERT_EQ(S_OK, injectedType.InjectTypeImplementationInModule(1));
}

TEST_F(InjectedTypeTest, CanReplaceBasicMethod)
{
	MockInjectedType injectedType(profilerInfoPtr, assemblyRegistry);

	const BYTE methodBytes[] = { 0x09, CEE_NOP, CEE_RET };
	LPCBYTE methodBytesStart = &methodBytes[0];
	const auto methodBytesSize = 0x3;

	EXPECT_CALL(profilerInfo, GetILFunctionBody(_, _, _, _))
		.WillOnce(DoAll(
			SetArrayArgument<2>(&methodBytesStart, &methodBytesStart + 3),
			SetArgPointee<3>(methodBytesSize),
			Return(S_OK)));

	BYTE buffer[500];
	memset(buffer, 0xFF, sizeof(buffer));

	MockMethodMalloc mockMethodMalloc;
	
	EXPECT_CALL(profilerInfo, GetILFunctionBodyAllocator(_, _))
		.WillOnce(DoAll(
			SetArgPointee<1>(&mockMethodMalloc),
			Return(S_OK)));

	EXPECT_CALL(mockMethodMalloc, Alloc(_))
		.WillOnce(Return(buffer));

	LPCBYTE methodBody;
	EXPECT_CALL(profilerInfo, SetILFunctionBody(_, _, _))
		.WillOnce(DoAll(
			SaveArg<2>(&methodBody),
			Return(S_OK)));

	InstructionList instructions;
	instructions.push_back(new Instruction(CEE_NOP));
	instructions.push_back(new Instruction(CEE_NOP));
	instructions.push_back(new Instruction(CEE_NOP));
	instructions.push_back(new Instruction(CEE_RET));

	ASSERT_EQ(S_OK, injectedType.ReplaceMethodWith(1, 1, instructions));

	// flags, size, max stack
	ASSERT_EQ(0x03, methodBody[0]);
	ASSERT_EQ(0x30, methodBody[1]);
	ASSERT_EQ(0x08, methodBody[2]);
	ASSERT_EQ(0x00, methodBody[3]);
	// code size
	ASSERT_EQ(0x04, methodBody[4]);
	ASSERT_EQ(0x00, methodBody[5]);
	ASSERT_EQ(0x00, methodBody[6]);
	ASSERT_EQ(0x00, methodBody[7]);
	// local function signature
	ASSERT_EQ(0x00, methodBody[8]);
	ASSERT_EQ(0x00, methodBody[9]);
	ASSERT_EQ(0x00, methodBody[10]);
	ASSERT_EQ(0x00, methodBody[11]);
	// instructions
	ASSERT_EQ(CEE_NOP, methodBody[12]);
	ASSERT_EQ(CEE_NOP, methodBody[13]);
	ASSERT_EQ(CEE_NOP, methodBody[14]);
	ASSERT_EQ(CEE_RET, methodBody[15]);
}

TEST_F(InjectedTypeTest, CanReplaceMethodWithLocalVariablesAndCustomStackSize)
{
	MockInjectedType injectedType(profilerInfoPtr, assemblyRegistry);

	const BYTE methodBytes[] = { 0x09, CEE_NOP, CEE_RET };
	LPCBYTE methodBytesStart = &methodBytes[0];
	const auto methodBytesSize = 0x3;

	EXPECT_CALL(profilerInfo, GetILFunctionBody(_, _, _, _))
		.WillOnce(DoAll(
			SetArrayArgument<2>(&methodBytesStart, &methodBytesStart + 3),
			SetArgPointee<3>(methodBytesSize),
			Return(S_OK)));

	BYTE buffer[500];
	memset(buffer, 0xFF, sizeof(buffer));

	MockMethodMalloc mockMethodMalloc;

	EXPECT_CALL(profilerInfo, GetILFunctionBodyAllocator(_, _))
		.WillOnce(DoAll(
			SetArgPointee<1>(&mockMethodMalloc),
			Return(S_OK)));

	EXPECT_CALL(mockMethodMalloc, Alloc(_))
		.WillOnce(Return(buffer));

	LPCBYTE methodBody;
	EXPECT_CALL(profilerInfo, SetILFunctionBody(_, _, _))
		.WillOnce(DoAll(
			SaveArg<2>(&methodBody),
			Return(S_OK)));

	InstructionList instructions;
	instructions.push_back(new Instruction(CEE_NOP));
	instructions.push_back(new Instruction(CEE_NOP));
	instructions.push_back(new Instruction(CEE_NOP));
	instructions.push_back(new Instruction(CEE_RET));

	ASSERT_EQ(S_OK, injectedType.ReplaceMethodWith(1, 1, instructions, 0x01020304, 0x20));

	// flags, size, max stack
	ASSERT_EQ(0x13, methodBody[0]);
	ASSERT_EQ(0x30, methodBody[1]);
	ASSERT_EQ(0x20, methodBody[2]);
	ASSERT_EQ(0x00, methodBody[3]);
	// code size
	ASSERT_EQ(0x04, methodBody[4]);
	ASSERT_EQ(0x00, methodBody[5]);
	ASSERT_EQ(0x00, methodBody[6]);
	ASSERT_EQ(0x00, methodBody[7]);
	// local function signature
	ASSERT_EQ(0x04, methodBody[8]);
	ASSERT_EQ(0x03, methodBody[9]);
	ASSERT_EQ(0x02, methodBody[10]);
	ASSERT_EQ(0x01, methodBody[11]);
	// instructions
	ASSERT_EQ(CEE_NOP, methodBody[12]);
	ASSERT_EQ(CEE_NOP, methodBody[13]);
	ASSERT_EQ(CEE_NOP, methodBody[14]);
	ASSERT_EQ(CEE_RET, methodBody[15]);
}

TEST_F(InjectedTypeTest, CanReplaceMethodWithExceptionHandler)
{
	MockInjectedType injectedType(profilerInfoPtr, assemblyRegistry);

	const BYTE methodBytes[] = { 0x09, CEE_NOP, CEE_RET };
	LPCBYTE methodBytesStart = &methodBytes[0];
	const auto methodBytesSize = 0x3;

	EXPECT_CALL(profilerInfo, GetILFunctionBody(_, _, _, _))
		.WillOnce(DoAll(
			SetArrayArgument<2>(&methodBytesStart, &methodBytesStart + 3),
			SetArgPointee<3>(methodBytesSize),
			Return(S_OK)));

	BYTE buffer[500];
	memset(buffer, 0xFF, sizeof(buffer));

	MockMethodMalloc mockMethodMalloc;

	EXPECT_CALL(profilerInfo, GetILFunctionBodyAllocator(_, _))
		.WillOnce(DoAll(
			SetArgPointee<1>(&mockMethodMalloc),
			Return(S_OK)));

	EXPECT_CALL(mockMethodMalloc, Alloc(_))
		.WillOnce(Return(buffer));

	LPCBYTE methodBody;
	EXPECT_CALL(profilerInfo, SetILFunctionBody(_, _, _))
		.WillOnce(DoAll(
			SaveArg<2>(&methodBody),
			Return(S_OK)));

	InstructionList instructions;
	instructions.push_back(new Instruction(CEE_NOP));
	instructions.push_back(new Instruction(CEE_NOP));
	instructions.push_back(new Instruction(CEE_NOP));
	instructions.push_back(new Instruction(CEE_NOP));
	instructions.push_back(new Instruction(CEE_RET));

	instructions[0]->m_offset = 0;
	instructions[1]->m_offset = 1;
	instructions[2]->m_offset = 2;

	auto exceptionHandler = new ExceptionHandler();
	exceptionHandler->SetTypedHandlerData(0x0100001E,
		instructions[0],
		instructions[1],
		instructions[1],
		instructions[2]);

	ExceptionHandlerList exceptionHandlerList;
	exceptionHandlerList.push_back(exceptionHandler);

	ASSERT_EQ(S_OK, injectedType.ReplaceMethodWith(1, 1, instructions, 0x01020304, 0x20, exceptionHandlerList));

	// flags, size, max stack
	ASSERT_EQ(0x1B, methodBody[0]);
	ASSERT_EQ(0x30, methodBody[1]);
	ASSERT_EQ(0x20, methodBody[2]);
	ASSERT_EQ(0x00, methodBody[3]);
	// code size
	ASSERT_EQ(0x05, methodBody[4]);
	ASSERT_EQ(0x00, methodBody[5]);
	ASSERT_EQ(0x00, methodBody[6]);
	ASSERT_EQ(0x00, methodBody[7]);
	// local function signature
	ASSERT_EQ(0x04, methodBody[8]);
	ASSERT_EQ(0x03, methodBody[9]);
	ASSERT_EQ(0x02, methodBody[10]);
	ASSERT_EQ(0x01, methodBody[11]);
	// instructions
	ASSERT_EQ(CEE_NOP, methodBody[12]);
	ASSERT_EQ(CEE_NOP, methodBody[13]);
	ASSERT_EQ(CEE_NOP, methodBody[14]);
	ASSERT_EQ(CEE_NOP, methodBody[15]);
	ASSERT_EQ(CEE_RET, methodBody[16]);
	ASSERT_EQ(0xFF, methodBody[17]); // padding
	ASSERT_EQ(0xFF, methodBody[18]); // padding
	ASSERT_EQ(0xFF, methodBody[19]); // padding
	// exceptions
	ASSERT_EQ(CorILMethod_Sect_FatFormat + CorILMethod_Sect_EHTable, methodBody[20]); // Section.Kind
	ASSERT_EQ(0x1C, methodBody[21]); // Section.DataSize
	ASSERT_EQ(0x0, methodBody[22]); // Section.DataSize
	ASSERT_EQ(0x0, methodBody[23]); // Section.DataSize
	ASSERT_EQ(0, *(reinterpret_cast<int*>(const_cast<LPBYTE>(methodBody + 24)))); // Handler Type
	ASSERT_EQ(0, *(reinterpret_cast<int*>(const_cast<LPBYTE>(methodBody + 28)))); // Try Begin
	ASSERT_EQ(1, *(reinterpret_cast<int*>(const_cast<LPBYTE>(methodBody + 32)))); // Try Offset
	ASSERT_EQ(1, *(reinterpret_cast<int*>(const_cast<LPBYTE>(methodBody + 36)))); // Handler Begin
	ASSERT_EQ(1, *(reinterpret_cast<int*>(const_cast<LPBYTE>(methodBody + 40)))); // Handler Offset
	ASSERT_EQ(0x0100001E, *(reinterpret_cast<int*>(const_cast<LPBYTE>(methodBody) + 44))); // Token
}