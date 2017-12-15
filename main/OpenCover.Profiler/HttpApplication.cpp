#include "stdafx.h"

#include "HttpApplication.h"
#include "AssemblyRegistry.h"

using namespace Instrumentation;
using namespace Injection;

namespace Context
{
	HttpApplication::HttpApplication(const ATL::CComPtr<ICorProfilerInfo>& profilerInfo,
		const std::shared_ptr<Injection::AssemblyRegistry>& assemblyRegistry) :
		InjectedType(profilerInfo, assemblyRegistry),
		m_typeDef(mdTypeDefNil),
		m_initMethodDef(mdMethodDefNil),
		m_onTraceContainerBeginRequestMethodDef(mdMethodDefNil),
		m_onTraceContainerEndRequestMethodDef(mdMethodDefNil),
		m_traceContainerGetCurrentRef(mdMemberRefNil),
		m_addBeginRequestRef(mdMemberRefNil),
		m_addEndRequestRef(mdMemberRefNil),
		m_eventHandlerCtorRef(mdMemberRefNil),
		m_notifyContextEndRef(mdMemberRefNil)
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

		ULONG ulCodeRVA = 0;
		auto httpApplication = mdTypeDefNil;
		auto httpApplicationCtor = mdMethodDefNil;
		GUARD_FAILURE_HRESULT(GetRVAFromKnownDefaultCtor(metaDataImport,
			L"System.Web.HttpApplication",
			&httpApplication,
			&httpApplicationCtor,
			&ulCodeRVA));

		COR_SIGNATURE sigInitMethod[] =
		{
			IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS,
			0x0,
			ELEMENT_TYPE_VOID
		};

		GUARD_FAILURE_HRESULT(metaDataImport->FindTypeDefByName(L"System.Web.HttpApplication", mdTokenNil, &m_typeDef));
		GUARD_FAILURE_HRESULT(metaDataImport->FindMethod(m_typeDef, L"Init", sigInitMethod, sizeof(sigInitMethod), &m_initMethodDef));

		mdModuleRef mscorlibRef;
		GUARD_FAILURE_HRESULT(DefineAssemblyMaxVersionRef(moduleId, L"mscorlib", &mscorlibRef));

		mdTypeRef eventArgsRef;
		GUARD_FAILURE_HRESULT(metaDataImport->FindTypeRef(mscorlibRef, L"System.EventArgs", &eventArgsRef));

		COR_SIGNATURE sigEventHandlerDelegate[] =
		{
			IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS,
			0x2,
			ELEMENT_TYPE_VOID,
			ELEMENT_TYPE_OBJECT,
			ELEMENT_TYPE_CLASS,
			0x0,0x0 // compressed token
		};
		auto sigEventHandlerDelegateLength = CorSigCompressAndCompactToken(eventArgsRef, sigEventHandlerDelegate, 5, 6, sizeof(sigEventHandlerDelegate));

		GUARD_FAILURE_HRESULT(metaDataEmit->DefineMethod(m_typeDef,
			L"OnTraceContainerBeginRequest",
			mdPrivate | mdHideBySig | mdReuseSlot,
			sigEventHandlerDelegate,
			sigEventHandlerDelegateLength,
			ulCodeRVA,
			0,
			&m_onTraceContainerBeginRequestMethodDef));

		GUARD_FAILURE_HRESULT(metaDataEmit->DefineMethod(m_typeDef,
			L"OnTraceContainerEndRequest",
			mdPrivate | mdHideBySig | mdReuseSlot,
			sigEventHandlerDelegate,
			sigEventHandlerDelegateLength,
			ulCodeRVA,
			0,
			&m_onTraceContainerEndRequestMethodDef));

		GUARD_FAILURE_HRESULT(RegisterImplementationTypeDependencies(moduleId, metaDataEmit, metaDataImport));
			
		return S_OK;
	}

	HRESULT HttpApplication::InjectTypeImplementation(ModuleID moduleId)
	{
		GUARD_FAILURE_HRESULT(InjectInitImplementation(moduleId));
		GUARD_FAILURE_HRESULT(InjectOnTraceContainerBeginRequestImplementation(moduleId));
		GUARD_FAILURE_HRESULT(InjectOnTraceContainerEndRequestImplementation(moduleId));

		return S_OK;
	}

	HRESULT HttpApplication::RegisterImplementationTypeDependencies(const ModuleID moduleId, ATL::CComPtr<IMetaDataEmit>& metaDataEmit, ATL::CComPtr<IMetaDataImport>& metaDataImport)
	{
		mdModuleRef mscorlibRef;
		GUARD_FAILURE_HRESULT(DefineAssemblyMaxVersionRef(moduleId, L"mscorlib", &mscorlibRef));

		mdTypeRef traceContainerBaseRef;
		GUARD_FAILURE_HRESULT(metaDataEmit->DefineTypeRefByName(mscorlibRef, L"TraceContainerBase", &traceContainerBaseRef));

		COR_SIGNATURE sigNotifyContextEnd[] =
		{
			IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS,
			0x0,
			ELEMENT_TYPE_VOID
		};

		GUARD_FAILURE_HRESULT(metaDataEmit->DefineMemberRef(traceContainerBaseRef, L"NotifyContextEnd", sigNotifyContextEnd, sizeof(sigNotifyContextEnd), &m_notifyContextEndRef));

		mdTypeRef traceContainerRef;
		GUARD_FAILURE_HRESULT(metaDataEmit->DefineTypeRefByName(mscorlibRef, L"TraceContainer", &traceContainerRef));

		COR_SIGNATURE sigGetCurrent[] =
		{
			IMAGE_CEE_CS_CALLCONV_DEFAULT,
			0x0,
			ELEMENT_TYPE_CLASS,
			0x00, 0x00 // compressed token
		};
		auto sigGetCurrentLength = CorSigCompressAndCompactToken(traceContainerBaseRef, sigGetCurrent, 3, 4, sizeof(sigGetCurrent));

		GUARD_FAILURE_HRESULT(metaDataEmit->DefineMemberRef(traceContainerRef, L"GetCurrent", sigGetCurrent, sigGetCurrentLength, &m_traceContainerGetCurrentRef));

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

		COR_SIGNATURE sigEventHandler[] =
		{
			IMAGE_CEE_CS_CALLCONV_DEFAULT | IMAGE_CEE_CS_CALLCONV_HASTHIS,
			0x1,
			ELEMENT_TYPE_VOID,
			ELEMENT_TYPE_CLASS,
			0x0,0x0 // compressed token
		};
		auto sigEventHandlerLength = CorSigCompressAndCompactToken(eventHandlerRef, sigEventHandler, 4, 5, sizeof(sigEventHandler));

		GUARD_FAILURE_HRESULT(metaDataEmit->DefineMemberRef(m_typeDef, L"add_EndRequest", sigEventHandler, sigEventHandlerLength, &m_addEndRequestRef));
		GUARD_FAILURE_HRESULT(metaDataEmit->DefineMemberRef(m_typeDef, L"add_BeginRequest", sigEventHandler, sigEventHandlerLength, &m_addBeginRequestRef));

		return S_OK;
	}

	HRESULT HttpApplication::InjectInitImplementation(ModuleID moduleId) const
	{
		InstructionList instructions;
		instructions.push_back(new Instruction(CEE_NOP));
		instructions.push_back(new Instruction(CEE_LDARG_0));
		instructions.push_back(new Instruction(CEE_LDARG_0));
		instructions.push_back(new Instruction(CEE_LDFTN, m_onTraceContainerBeginRequestMethodDef));
		instructions.push_back(new Instruction(CEE_NEWOBJ, m_eventHandlerCtorRef));
		instructions.push_back(new Instruction(CEE_CALL, m_addBeginRequestRef));
		instructions.push_back(new Instruction(CEE_NOP));

		instructions.push_back(new Instruction(CEE_LDARG_0));
		instructions.push_back(new Instruction(CEE_LDARG_0));
		instructions.push_back(new Instruction(CEE_LDFTN, m_onTraceContainerEndRequestMethodDef));
		instructions.push_back(new Instruction(CEE_NEWOBJ, m_eventHandlerCtorRef));
		instructions.push_back(new Instruction(CEE_CALL, m_addEndRequestRef));
		instructions.push_back(new Instruction(CEE_NOP));

		instructions.push_back(new Instruction(CEE_RET));

		GUARD_FAILURE_HRESULT(ReplaceMethodWith(moduleId, m_initMethodDef, instructions));

		return S_OK;
	}

	HRESULT HttpApplication::InjectOnTraceContainerBeginRequestImplementation(const ModuleID moduleId) const
	{
		InstructionList instructions;
		instructions.push_back(new Instruction(CEE_NOP));
		instructions.push_back(new Instruction(CEE_RET));

		GUARD_FAILURE_HRESULT(ReplaceMethodWith(moduleId, m_onTraceContainerBeginRequestMethodDef, instructions));

		return S_OK;
	}

	HRESULT HttpApplication::InjectOnTraceContainerEndRequestImplementation(const ModuleID moduleId) const
	{
		InstructionList instructions;
		instructions.push_back(new Instruction(CEE_NOP));
		instructions.push_back(new Instruction(CEE_CALL, m_traceContainerGetCurrentRef));
		instructions.push_back(new Instruction(CEE_CALLVIRT, m_notifyContextEndRef));
		instructions.push_back(new Instruction(CEE_NOP));
		instructions.push_back(new Instruction(CEE_RET));

		GUARD_FAILURE_HRESULT(ReplaceMethodWith(moduleId, m_onTraceContainerEndRequestMethodDef, instructions));

		return S_OK;
	}
}
