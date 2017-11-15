#include "stdafx.h"
#include <tchar.h>

#include "ReleaseTrace.h"

#include "Method.h"
#include "InjectedType.h"

using namespace Instrumentation;

namespace Injection
{
	static COR_SIGNATURE ctorSignature[] =
	{
		IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS,
		0x00,
		ELEMENT_TYPE_VOID
	};

	InjectedType::InjectedType(const ATL::CComPtr<ICorProfilerInfo>& profilerInfo,
		const std::shared_ptr<AssemblyRegistry>& assemblyRegistry) : 
			m_profilerInfo(profilerInfo),
			m_assemblyRegistry(assemblyRegistry),
			m_registrationMap()
	{
	}

	bool InjectedType::IsRegistered() const
	{
		for (auto const &iter: m_registrationMap)
		{
			if (iter.second)
			{
				return true;
			}
		}
		return false;
	}

	HRESULT InjectedType::RegisterTypeInModule(const ModuleID moduleId)
	{
		if (!ShouldRegisterType(moduleId))
		{
			return S_FALSE;
		}
		
		const auto registerTypeResult = RegisterType(moduleId);
		m_registrationMap[moduleId] = registerTypeResult == S_OK;

		return registerTypeResult;
	}

	HRESULT InjectedType::InjectTypeImplementationInModule(const ModuleID moduleId)
	{
		if (!m_registrationMap[moduleId])
		{
			return E_ILLEGAL_METHOD_CALL;
		}
		return InjectTypeImplementation(moduleId);
	}

	HRESULT InjectedType::GetMetaDataImport(const ModuleID moduleId, ATL::CComPtr<IMetaDataImport>& metaDataImport) const
	{
		auto result = m_profilerInfo->GetModuleMetaData(moduleId,
			ofRead | ofWrite, IID_IMetaDataImport, reinterpret_cast<IUnknown**>(&metaDataImport));

		return SUCCEEDED(result) ? S_OK : result;
	}

	HRESULT InjectedType::GetMetaDataEmit(const ModuleID moduleId, ATL::CComPtr<IMetaDataEmit>& metaDataEmit) const
	{
		auto result = m_profilerInfo->GetModuleMetaData(moduleId,
			ofRead | ofWrite, IID_IMetaDataEmit, reinterpret_cast<IUnknown**>(&metaDataEmit));

		return SUCCEEDED(result) ? S_OK : result;
	}

	bool InjectedType::HasTypeDef(const ModuleID moduleId, const LPCWSTR typeDefName) const
	{
		ATL::CComPtr<IMetaDataImport> metaDataImport;
		auto result = GetMetaDataImport(moduleId, metaDataImport);
		if (result != S_OK)
		{
			return false;
		}

		mdTypeDef systemObject = mdTokenNil;
		result = metaDataImport->FindTypeDefByName(typeDefName, mdTokenNil, &systemObject);
		return result == S_OK;
	}

	HRESULT InjectedType::ReplaceMethodWith(const ModuleID moduleId, const mdToken functionToken, InstructionList &instructions, const mdSignature localVarSigTok, const unsigned minimumStackSize) const
	{
		ExceptionHandlerList exceptionHandlerList;
		return ReplaceMethodWith(moduleId, functionToken, instructions, localVarSigTok, minimumStackSize, exceptionHandlerList);
	}

	HRESULT InjectedType::ReplaceMethodWith(const ModuleID moduleId, const mdToken functionToken, InstructionList &instructions, const mdSignature localVarSigTok, const unsigned minimumStackSize, ExceptionHandlerList &exceptions) const
	{
		IMAGE_COR_ILMETHOD* pMethodHeader = nullptr;
		ULONG iMethodSize = 0;
		GUARD_FAILURE_HRESULT(m_profilerInfo->GetILFunctionBody(moduleId, functionToken, (LPCBYTE*)&pMethodHeader, &iMethodSize));

		Method method(pMethodHeader);
		method.DeleteAllInstructions();
		method.AppendInstructions(instructions);
		method.SetMinimumStackSize(minimumStackSize);

		if (exceptions.size() > 0)
		{
			method.AddExceptionHandlers(exceptions);
		}

		ATL::CComPtr<IMethodMalloc> methodMalloc;
		GUARD_FAILURE_HRESULT(m_profilerInfo->GetILFunctionBodyAllocator(moduleId, &methodMalloc));

		IMAGE_COR_ILMETHOD* pNewMethod = static_cast<IMAGE_COR_ILMETHOD*>(methodMalloc->Alloc(method.GetMethodSize()));
		method.WriteMethod(pNewMethod);

		if (localVarSigTok != mdSignatureNil)
		{
			pNewMethod->Fat.Flags |= CorILMethod_InitLocals; // always added when local variables present: http://www.liranchen.com/2010/07/behind-locals-init-flag.html
			pNewMethod->Fat.LocalVarSigTok = localVarSigTok;
		}

		GUARD_FAILURE_HRESULT(m_profilerInfo->SetILFunctionBody(moduleId, functionToken, (LPCBYTE)pNewMethod));

		return S_OK;
	}

	ULONG InjectedType::CorSigCompressAndCompactToken(const mdToken tk, const PCOR_SIGNATURE sig, const int indexStart, const int indexEnd, const int length)
	{
		const auto uncompressedByteCount = indexEnd - indexStart + 1;
		const auto compressedByteCount = static_cast<int>(CorSigCompressToken(tk, &sig[indexStart]));
		const auto extraBytes = (indexEnd - indexStart + 1) - compressedByteCount;

		for (auto i = indexStart + compressedByteCount, j = indexStart + uncompressedByteCount; j < length; i++, j++)
		{
			sig[i] = sig[j];
		}
		return length - extraBytes;
	}

	HRESULT InjectedType::GetRVAFromKnownDefaultCtor(ATL::CComPtr<IMetaDataImport>& metaDataImport,
		const LPCWSTR knownTypeDefName,
		mdTypeDef* pKnownTypeDef,
		mdMethodDef* pKnownTypeDefaultCtorDef,
		ULONG* pCodeRVA)
	{
		GUARD_FAILURE_HRESULT(metaDataImport->FindTypeDefByName(knownTypeDefName, mdTokenNil, pKnownTypeDef));
		GUARD_FAILURE_HRESULT(metaDataImport->FindMethod(*pKnownTypeDef, L".ctor", ctorSignature, sizeof(ctorSignature), pKnownTypeDefaultCtorDef));
		GUARD_FAILURE_HRESULT(metaDataImport->GetMethodProps(*pKnownTypeDefaultCtorDef, nullptr, nullptr, 0, nullptr, nullptr, nullptr, nullptr, pCodeRVA, nullptr));

		return S_OK;
	}
}