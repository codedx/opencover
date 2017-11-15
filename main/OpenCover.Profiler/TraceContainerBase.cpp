#include "stdafx.h"
#include <tchar.h>

#include "ReleaseTrace.h"

#include "TraceContainerBase.h"
#include "Instruction.h"

using namespace Instrumentation;
using namespace Injection;

namespace Context
{
	TraceContainerBase::TraceContainerBase(const ATL::CComPtr<ICorProfilerInfo>& profilerInfo,
		const std::shared_ptr<AssemblyRegistry>& assemblyRegistry) :
			InjectedType(profilerInfo, assemblyRegistry),
			m_typeDef(mdTypeDefNil),
			m_ctorMethodDef(mdMethodDefNil),
			m_contextIdHighFieldDef(mdFieldDefNil),
			m_contextIdLowFieldDef(mdFieldDefNil),
			m_contextIdFieldDef(mdFieldDefNil),
			m_systemObject(mdTypeDefNil),
			m_systemObjectCtor(mdMethodDefNil),
			m_guidTypeDef(mdTypeDefNil),
			m_guidNewGuidMethodDef(mdMethodDefNil),
			m_guidToByteArrayMethodDef(mdMethodDefNil),
			m_bitConverterToUInt64MethodDef(mdMethodDefNil),
			m_ctorLocalVariablesSignature(mdSignatureNil)
	{
	}

	TraceContainerBase::~TraceContainerBase()
	{
	}

	bool TraceContainerBase::ShouldRegisterType(const ModuleID moduleId) const
	{
		return HasTypeDef(moduleId, L"System.Object");
	}

	HRESULT TraceContainerBase::RegisterType(const ModuleID moduleId)
	{
		ATL::CComPtr<IMetaDataImport> metaDataImport;
		GUARD_FAILURE_HRESULT(GetMetaDataImport(moduleId, metaDataImport));

		ATL::CComPtr<IMetaDataEmit> metaDataEmit;
		GUARD_FAILURE_HRESULT(GetMetaDataEmit(moduleId, metaDataEmit));

		ULONG ulCodeRVA = 0;
		GUARD_FAILURE_HRESULT(GetRVAFromKnownDefaultCtor(metaDataImport,
			L"System.Object",
			&m_systemObject,
			&m_systemObjectCtor,
			&ulCodeRVA));

		GUARD_FAILURE_HRESULT(metaDataEmit->DefineTypeDef(L"TraceContainerBase",
			tdClass | tdPublic | tdAutoLayout | tdAnsiClass | tdBeforeFieldInit,
			m_systemObject,
			nullptr,
			&m_typeDef));

		GUARD_FAILURE_HRESULT(metaDataImport->FindTypeDefByName(L"System.Guid", mdTokenNil, &m_guidTypeDef));

		COR_SIGNATURE sigGuidField[] =
		{
			IMAGE_CEE_CS_CALLCONV_FIELD,
			ELEMENT_TYPE_VALUETYPE,
			0x0,0x0 // compressed token
		};

		auto sigGuidFieldLength = CorSigCompressAndCompactToken(m_guidTypeDef, sigGuidField, 2, 3, sizeof(sigGuidField));
		GUARD_FAILURE_HRESULT(metaDataEmit->DefineField(m_typeDef,
			L"_contextId",
			fdPublic | fdInitOnly,
			sigGuidField,
			sigGuidFieldLength,
			0,
			nullptr,
			0,
			&m_contextIdFieldDef));

		COR_SIGNATURE sigUInt64Field[] =
		{
			IMAGE_CEE_CS_CALLCONV_FIELD,
			ELEMENT_TYPE_U8
		};

		ULONG sigUInt64FieldLength = sizeof(sigUInt64Field);

		GUARD_FAILURE_HRESULT(metaDataEmit->DefineField(m_typeDef,
			L"_contextIdHigh",
			fdPublic | fdInitOnly,
			sigUInt64Field,
			sigUInt64FieldLength,
			0,
			nullptr,
			0,
			&m_contextIdHighFieldDef));

		GUARD_FAILURE_HRESULT(metaDataEmit->DefineField(m_typeDef,
			L"_contextIdLow",
			fdPublic | fdInitOnly,
			sigUInt64Field,
			sigUInt64FieldLength,
			0,
			nullptr,
			0,
			&m_contextIdLowFieldDef));

		COR_SIGNATURE sigCtorMethod[] =
		{
			IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS,
			0x0,
			ELEMENT_TYPE_VOID
		};

		GUARD_FAILURE_HRESULT(metaDataEmit->DefineMethod(m_typeDef,
			L".ctor",
			mdPublic | mdHideBySig | mdSpecialName | mdRTSpecialName,
			sigCtorMethod,
			sizeof(sigCtorMethod),
			ulCodeRVA,
			0,
			&m_ctorMethodDef));

		COR_SIGNATURE sigCtorLocalVariables[] =
		{
			IMAGE_CEE_CS_CALLCONV_LOCAL_SIG,
			0x2, // skipping CorSigCompressData (already one byte)
			ELEMENT_TYPE_VALUETYPE,
			0x0,0x0, // GUID Type (compressed token)
			ELEMENT_TYPE_ARRAY,
			ELEMENT_TYPE_U8, // Array Type
			0x1, // Rank (compressed data) // skipping CorSigCompressData (already one byte)
			0x0 // NumSizes (compressed data) // skipping CorSigCompressData (already one byte)
		};

		auto sigCtorLocalVariablesLength = CorSigCompressAndCompactToken(m_guidTypeDef, sigCtorLocalVariables, 3, 4, sizeof(sigCtorLocalVariables));
		GUARD_FAILURE_HRESULT(metaDataEmit->GetTokenFromSig(sigCtorLocalVariables, sigCtorLocalVariablesLength, &m_ctorLocalVariablesSignature));

		GUARD_FAILURE_HRESULT(RegisterImplementationTypeDependencies(moduleId, metaDataImport));

		return S_OK;
	}

	HRESULT TraceContainerBase::InjectTypeImplementation(const ModuleID moduleId)
	{
		ATL::CComPtr<IMetaDataEmit> metaDataEmit;
		GUARD_FAILURE_HRESULT(GetMetaDataEmit(moduleId, metaDataEmit));
		GUARD_FAILURE_HRESULT(InjectCtorImplementation(moduleId, metaDataEmit));

		return S_OK;
	}

	HRESULT TraceContainerBase::RegisterImplementationTypeDependencies(const ModuleID moduleId, ATL::CComPtr<IMetaDataImport>& metaDataImport)
	{
		COR_SIGNATURE sigNewGuidMethod[] =
		{
			IMAGE_CEE_CS_CALLCONV_DEFAULT,
			0x0,
			ELEMENT_TYPE_VALUETYPE,
			0x0,0x0 // compressed token
		};

		auto sigNewGuidMethodLength = CorSigCompressAndCompactToken(m_guidTypeDef, sigNewGuidMethod, 3, 4, sizeof(sigNewGuidMethod));
		GUARD_FAILURE_HRESULT(metaDataImport->FindMethod(m_guidTypeDef, L"NewGuid", sigNewGuidMethod, sigNewGuidMethodLength, &m_guidNewGuidMethodDef));

		COR_SIGNATURE sigToByteArrayMethod[] =
		{
			IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS,
			0x0,
			ELEMENT_TYPE_SZARRAY,
			ELEMENT_TYPE_U1
		};

		GUARD_FAILURE_HRESULT(metaDataImport->FindMethod(m_guidTypeDef, L"ToByteArray", sigToByteArrayMethod, sizeof(sigToByteArrayMethod), &m_guidToByteArrayMethodDef));

		mdTypeDef bitConverterToken;
		GUARD_FAILURE_HRESULT(metaDataImport->FindTypeDefByName(L"System.BitConverter", mdTokenNil, &bitConverterToken));

		COR_SIGNATURE sigToUInt64Method[] =
		{
			IMAGE_CEE_CS_CALLCONV_DEFAULT,
			0x2,
			ELEMENT_TYPE_U8,
			ELEMENT_TYPE_SZARRAY,
			ELEMENT_TYPE_U1,
			ELEMENT_TYPE_I4
		};

		GUARD_FAILURE_HRESULT(metaDataImport->FindMethod(bitConverterToken, L"ToUInt64", sigToUInt64Method, sizeof(sigToUInt64Method), &m_bitConverterToUInt64MethodDef));

		return S_OK;
	}

	HRESULT TraceContainerBase::InjectCtorImplementation(const ModuleID moduleId, ATL::CComPtr<IMetaDataEmit>& metaDataEmit) const
	{
		InstructionList instructions;

		instructions.push_back(new Instruction(CEE_LDARG_0));
		instructions.push_back(new Instruction(CEE_CALL, m_systemObjectCtor));
		instructions.push_back(new Instruction(CEE_NOP));
		instructions.push_back(new Instruction(CEE_NOP));

		instructions.push_back(new Instruction(CEE_CALL, m_guidNewGuidMethodDef));
		instructions.push_back(new Instruction(CEE_STLOC_0));

		instructions.push_back(new Instruction(CEE_LDLOCA_S, 0));
		instructions.push_back(new Instruction(CEE_CALL, m_guidToByteArrayMethodDef));
		instructions.push_back(new Instruction(CEE_STLOC_1));

		instructions.push_back(new Instruction(CEE_LDARG_0));
		instructions.push_back(new Instruction(CEE_LDLOC_1));
		instructions.push_back(new Instruction(CEE_LDC_I4_0));
		instructions.push_back(new Instruction(CEE_CALL, m_bitConverterToUInt64MethodDef));
		instructions.push_back(new Instruction(CEE_STFLD, m_contextIdLowFieldDef));

		instructions.push_back(new Instruction(CEE_LDARG_0));
		instructions.push_back(new Instruction(CEE_LDLOC_1));
		instructions.push_back(new Instruction(CEE_LDC_I4_8));
		instructions.push_back(new Instruction(CEE_CALL, m_bitConverterToUInt64MethodDef));
		instructions.push_back(new Instruction(CEE_STFLD, m_contextIdHighFieldDef));

		instructions.push_back(new Instruction(CEE_LDARG_0));
		instructions.push_back(new Instruction(CEE_LDLOC_0));
		instructions.push_back(new Instruction(CEE_STFLD, m_contextIdFieldDef));
		instructions.push_back(new Instruction(CEE_RET));

		GUARD_FAILURE_HRESULT(ReplaceMethodWith(moduleId, m_ctorMethodDef, instructions, m_ctorLocalVariablesSignature));

		return S_OK;
	}
}