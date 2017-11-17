#include "stdafx.h"

#include "InjectedType.h"
#include "TraceContainerBase.h"
#include "InjectedTypeTestFixture.h"
#include "MockICorProfilerInfo.h"
#include "MockIMetaDataImport.h"

using namespace Injection;
using namespace Context;
using namespace testing;
using namespace std;

class TraceContainerBaseTest : public InjectedTypeTestFixture 
{
protected:
	void SetUp() override
	{
		InjectedTypeTestFixture::SetUp();
	}

	void SetUpGetModuleMetaData(const bool isSuccessCase)
	{
		if (isSuccessCase)
		{
			EXPECT_CALL(profilerInfo, GetModuleMetaData(_, _, IID_IMetaDataImport, _))
				.WillOnce(DoAll(SetArgPointee<3>(&metaDataImport), Return(S_OK)));
		}
		else
		{
			EXPECT_CALL(profilerInfo, GetModuleMetaData(_, _, IID_IMetaDataImport, _))
				.WillOnce(DoAll(SetArgPointee<3>(&metaDataImport), Return(S_OK)))
				.WillOnce(Return(shortCircuitRetVal));
		}
	}

	void SetUpFindTypeDefByName(const bool isSuccessCase)
	{
		if (isSuccessCase)
		{
			EXPECT_CALL(metaDataImport, FindTypeDefByName(L"System.Object", _, _))
				.WillOnce(Return(S_OK));
		}
		else
		{
			EXPECT_CALL(metaDataImport, FindTypeDefByName(L"System.Object", _, _))
				.WillOnce(Return(E_FAIL));
		}
	}

	const int shortCircuitRetVal = -0x123;

	MockIMetaDataImport metaDataImport;
};

TEST_F(TraceContainerBaseTest, RegisterTypeSkippedForUnrelatedModule)
{
	TraceContainerBase traceContainerBase(profilerInfoPtr, assemblyRegistry, 0x12345678);

	SetUpGetModuleMetaData(true);
	SetUpFindTypeDefByName(false);
	
	ASSERT_EQ(S_FALSE, traceContainerBase.RegisterTypeInModule(1));
}

TEST_F(TraceContainerBaseTest, RegisterTypeOccursForRelatedModule)
{
	TraceContainerBase traceContainerBase(profilerInfoPtr, assemblyRegistry, 0x12345678);

	SetUpGetModuleMetaData(false);
	SetUpFindTypeDefByName(true);

	ASSERT_EQ(shortCircuitRetVal, traceContainerBase.RegisterTypeInModule(1));
}

TEST_F(TraceContainerBaseTest, InjectTypeImplementationFailsIfRegisterTypeFails)
{
	TraceContainerBase traceContainerBase(profilerInfoPtr, assemblyRegistry, 0x12345678);

	SetUpGetModuleMetaData(false);
	SetUpFindTypeDefByName(true);

	ASSERT_EQ(shortCircuitRetVal, traceContainerBase.RegisterTypeInModule(1));
	ASSERT_EQ(E_ILLEGAL_METHOD_CALL, traceContainerBase.InjectTypeImplementationInModule(1));
}

TEST_F(TraceContainerBaseTest, InjectTypeImplementationFailsIfRegisterTypeNotCalled)
{
	TraceContainerBase traceContainerBase(profilerInfoPtr, assemblyRegistry, 0x12345678);
	ASSERT_EQ(E_ILLEGAL_METHOD_CALL, traceContainerBase.InjectTypeImplementationInModule(1));
}