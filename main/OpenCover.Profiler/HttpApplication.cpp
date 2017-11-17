#include "stdafx.h"

#include "HttpApplication.h"
#include "AssemblyRegistry.h"

using namespace Instrumentation;
using namespace Injection;

namespace Context
{
	HttpApplication::HttpApplication(const ATL::CComPtr<ICorProfilerInfo>& profilerInfo,
		const std::shared_ptr<Injection::AssemblyRegistry>& assemblyRegistry,
		const std::shared_ptr<TraceContainerBase>& traceContainerBase) :
		InjectedType(profilerInfo, assemblyRegistry),
		m_typeDef(mdTypeDefNil),
		m_initMethodDef(mdMethodDefNil),
		m_addEndRequestRef(mdMemberRefNil),
		m_eventHandlerCtorRef(mdMemberRefNil),
		m_onContextEndRef(mdMemberRefNil)
	{
		
	}

	bool HttpApplication::ShouldRegisterType(const ModuleID moduleId) const
	{
		if (!HasTypeDef(moduleId, L"System.Web.HttpApplication"))
		{
			return false;
		}

		AssemblyReference mscorlibReference;
		if (m_assemblyRegistry->FindMaxAssemblyVersion(L"mscorlib", mscorlibReference))
		{
			// EndRequestEvent available since .NET 1.1
			AssemblyVersion minimumDotNetVersion;
			minimumDotNetVersion.majorVersion = 1;
			minimumDotNetVersion.minorVersion = 1;
			minimumDotNetVersion.buildNumber = 0;
			minimumDotNetVersion.revisionNumber = 0;

			return mscorlibReference.version >= minimumDotNetVersion;
		}
		return false;
	}

	HRESULT HttpApplication::RegisterType(const ModuleID moduleId)
	{
		ATL::CComPtr<IMetaDataImport> metaDataImport;
		GUARD_FAILURE_HRESULT(GetMetaDataImport(moduleId, metaDataImport));

		ATL::CComPtr<IMetaDataEmit> metaDataEmit;
		GUARD_FAILURE_HRESULT(GetMetaDataEmit(moduleId, metaDataEmit));

		COR_SIGNATURE sigInitMethod[] =
		{
			IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS,
			0x0,
			ELEMENT_TYPE_VOID
		};

		GUARD_FAILURE_HRESULT(metaDataImport->FindTypeDefByName(L"System.Web.HttpApplication", mdTokenNil, &m_typeDef));
		GUARD_FAILURE_HRESULT(metaDataImport->FindMethod(m_typeDef, L"Init", sigInitMethod, sizeof(sigInitMethod), &m_initMethodDef));

		GUARD_FAILURE_HRESULT(RegisterImplementationTypeDependencies(moduleId, metaDataEmit, metaDataImport));
			
		return S_OK;
	}

	HRESULT HttpApplication::InjectTypeImplementation(ModuleID moduleId)
	{
		GUARD_FAILURE_HRESULT(InjectInitImplementation(moduleId));

		return S_OK;
	}

	HRESULT HttpApplication::RegisterImplementationTypeDependencies(const ModuleID moduleId, ATL::CComPtr<IMetaDataEmit>& metaDataEmit, ATL::CComPtr<IMetaDataImport>& metaDataImport)
	{
		mdModuleRef mscorlibRef;
		GUARD_FAILURE_HRESULT(DefineAssemblyMaxVersionRef(moduleId, L"mscorlib", &mscorlibRef));

		mdTypeRef traceContainerRef;
		GUARD_FAILURE_HRESULT(metaDataEmit->DefineTypeRefByName(mscorlibRef, L"TraceContainerBase", &traceContainerRef));

		mdTypeRef eventArgsRef;
		GUARD_FAILURE_HRESULT(metaDataEmit->DefineTypeRefByName(mscorlibRef, L"System.EventArgs", &eventArgsRef));

		COR_SIGNATURE sigOnEndRequestHttpApplication[] =
		{
			IMAGE_CEE_CS_CALLCONV_DEFAULT,
			0x2,
			ELEMENT_TYPE_VOID,
			ELEMENT_TYPE_OBJECT,
			ELEMENT_TYPE_CLASS,
			0x0,0x0 // compressed token
		};
		auto sigOnEndRequestHttpApplicationLength = CorSigCompressAndCompactToken(eventArgsRef, sigOnEndRequestHttpApplication, 5, 6, sizeof(sigOnEndRequestHttpApplication));

		GUARD_FAILURE_HRESULT(metaDataEmit->DefineMemberRef(traceContainerRef, L"OnContextEnd", sigOnEndRequestHttpApplication, sigOnEndRequestHttpApplicationLength, &m_onContextEndRef));

		mdTypeRef eventHandlerRef;
		GUARD_FAILURE_HRESULT(metaDataEmit->DefineTypeRefByName(mscorlibRef, L"System.EventHandler", &eventHandlerRef));

		COR_SIGNATURE sigEventHandlerConstructor[] =
		{
			IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS,
			0x2,
			ELEMENT_TYPE_VOID,
			ELEMENT_TYPE_OBJECT,
			ELEMENT_TYPE_I // native int
		};
		GUARD_FAILURE_HRESULT(metaDataEmit->DefineMemberRef(eventHandlerRef, L".ctor", sigEventHandlerConstructor, sizeof(sigEventHandlerConstructor), &m_eventHandlerCtorRef));

		COR_SIGNATURE sigAddEndRequest[] =
		{
			IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS,
			0x1,
			ELEMENT_TYPE_VOID,
			ELEMENT_TYPE_CLASS,
			0x0,0x0 // compressed token
		};
		auto sigAddEndRequestLength = CorSigCompressAndCompactToken(eventHandlerRef, sigAddEndRequest, 4, 5, sizeof(sigAddEndRequest));

		GUARD_FAILURE_HRESULT(metaDataEmit->DefineMemberRef(m_typeDef, L"add_EndRequest", sigAddEndRequest, sigAddEndRequestLength, &m_addEndRequestRef));

		return S_OK;
	}

	HRESULT HttpApplication::InjectInitImplementation(ModuleID moduleId) const
	{
		InstructionList instructions;
		instructions.push_back(new Instruction(CEE_NOP));
		instructions.push_back(new Instruction(CEE_LDARG_0));
		instructions.push_back(new Instruction(CEE_LDNULL));
		instructions.push_back(new Instruction(CEE_LDFTN, m_onContextEndRef));
		instructions.push_back(new Instruction(CEE_NEWOBJ, m_eventHandlerCtorRef));
		instructions.push_back(new Instruction(CEE_CALL, m_addEndRequestRef));
		instructions.push_back(new Instruction(CEE_NOP));
		instructions.push_back(new Instruction(CEE_RET));

		GUARD_FAILURE_HRESULT(ReplaceMethodWith(moduleId, m_initMethodDef, instructions));

		return S_OK;
	}
}
