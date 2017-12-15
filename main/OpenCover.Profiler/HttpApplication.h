#pragma once
#include "InjectedType.h"

namespace Context
{
	class HttpApplication :
		public Injection::InjectedType
	{
	public:
		HttpApplication(const ATL::CComPtr<ICorProfilerInfo>& profilerInfo,
			const std::shared_ptr<Injection::AssemblyRegistry>& assemblyRegistry);

		virtual ~HttpApplication()
		{
		}

	private:
		bool ShouldRegisterType(const ModuleID moduleId) const override;
		HRESULT RegisterType(const ModuleID moduleId) override;

		HRESULT InjectTypeImplementation(ModuleID moduleId) override;

		HRESULT RegisterImplementationTypeDependencies(const ModuleID moduleId, ATL::CComPtr<IMetaDataEmit>& metaDataEmit, ATL::CComPtr<IMetaDataImport>& metaDataImport);

		HRESULT InjectInitImplementation(const ModuleID moduleId) const;
		HRESULT InjectOnTraceContainerBeginRequestImplementation(const ModuleID moduleId) const;
		HRESULT InjectOnTraceContainerEndRequestImplementation(const ModuleID moduleId) const;

		mdTypeDef m_typeDef;
		mdMethodDef m_initMethodDef;
		mdMethodDef m_onTraceContainerBeginRequestMethodDef;
		mdMethodDef m_onTraceContainerEndRequestMethodDef;

		mdMemberRef m_traceContainerGetCurrentRef;
		mdMemberRef m_addBeginRequestRef;
		mdMemberRef m_addEndRequestRef;
		mdMemberRef m_eventHandlerCtorRef;
		mdMemberRef m_notifyContextEndRef;
	};
}
