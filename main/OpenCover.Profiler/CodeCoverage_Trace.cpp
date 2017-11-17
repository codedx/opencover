//
// OpenCover
//
// This source code is released under the MIT License; see the accompanying license file.
//
// CodeCoverage.cpp : Implementation of CCodeCoverage

#include "stdafx.h"
#include "AssemblyRegistry.h"
#include "Instruction.h"
#include "CodeCoverage.h"

using namespace Instrumentation;

HRESULT CCodeCoverage::RegisterTraceTypes(const ModuleID moduleId)
{
	GUARD_FAILURE_HRESULT(m_assemblyRegistry->RecordAssemblyMetadataForModule(moduleId));
	GUARD_FAILURE_HRESULT(RegisterInjectedType(moduleId, *m_traceContainerCallContext));
	GUARD_FAILURE_HRESULT(RegisterInjectedType(moduleId, *m_httpApplication));
	return S_OK;
}

HRESULT CCodeCoverage::RegisterInjectedType(const ModuleID moduleId, Injection::InjectedType& type)
{
	const auto typeRegisteredResult = type.RegisterTypeInModule(moduleId);
	GUARD_FAILURE_HRESULT(typeRegisteredResult);

	if (typeRegisteredResult == S_FALSE)
	{
		return S_OK;
	}

	GUARD_FAILURE_HRESULT(type.InjectTypeImplementationInModule(moduleId));
	return S_OK;
}

/// <summary>This is the body of our method marked with the SecuritySafeCriticalAttribute</summary>
/// <remarks>Calls the method that is marked with the SecurityCriticalAttribute</remarks>
HRESULT CCodeCoverage::AddTraceSafeCuckooBody(ModuleID moduleId)
{
	ATLTRACE(_T("::AddSafeCuckooBody => Adding SafeVisited..."));

	// Define local variables for GetCurrent
	COR_SIGNATURE sigLocalVariable[] =
	{
		IMAGE_CEE_CS_CALLCONV_LOCAL_SIG,
		0x1, // skipping CorSigCompressData (already one byte)
		ELEMENT_TYPE_CLASS,
		0x0,0x0	// TraceContainer
	};
	auto sigLocalVariableLength = (ULONG)sizeof(sigLocalVariable);
	const auto compressedCount = CorSigCompressToken(m_traceContainerCallContext->GetType(), &sigLocalVariable[3]);
	sigLocalVariableLength = sigLocalVariableLength - 2 + compressedCount;

	CComPtr<IMetaDataEmit> metaDataEmit;
	COM_FAIL_MSG_RETURN_OTHER(m_profilerInfo2->GetModuleMetaData(moduleId, ofWrite, IID_IMetaDataEmit, (IUnknown**)&metaDataEmit), 0,
		_T("    ::AddSafeCuckooBody(ModuleId) => GetModuleMetaData => 0x%X"));

	mdSignature localVariableSignature;
	COM_FAIL_MSG_RETURN_OTHER(metaDataEmit->GetTokenFromSig(sigLocalVariable, sigLocalVariableLength, &localVariableSignature), 0,
		_T("    ::AddSafeCuckooBody(ModuleId) => GetTokenFromSig => 0x%X"));

	InstructionList instructions;
	instructions.push_back(new Instruction(CEE_NOP));
	instructions.push_back(new Instruction(CEE_CALL, m_traceContainerCallContext->GetCurrentMethod()));
	instructions.push_back(new Instruction(CEE_STLOC_0));
	instructions.push_back(new Instruction(CEE_LDARG_0));
	instructions.push_back(new Instruction(CEE_LDLOC_0));
	instructions.push_back(new Instruction(CEE_LDFLD, m_traceContainerCallContext->GetContextIdHighField()));
	instructions.push_back(new Instruction(CEE_LDLOC_0));
	instructions.push_back(new Instruction(CEE_LDFLD, m_traceContainerCallContext->GetContextIdLowField()));
	instructions.push_back(new Instruction(CEE_CALL, m_cuckooCriticalToken));
	instructions.push_back(new Instruction(CEE_RET));

	InstrumentMethodWith(moduleId, m_cuckooSafeToken, instructions, localVariableSignature);

	ATLTRACE(_T("::AddSafeCuckooBody => Adding SafeVisited - Done!"));

	return S_OK;
}
