#pragma once
#include <memory>

#include "InjectedType.h"

namespace Context
{
	class TraceContainerBase;

	class TraceContainerCallContext :
		public Injection::InjectedType
	{
	public:
		TraceContainerCallContext(const ATL::CComPtr<ICorProfilerInfo>& profilerInfo,
			const std::shared_ptr<Injection::AssemblyRegistry>& assemblyRegistry,
			const std::shared_ptr<TraceContainerBase>& traceContainerBase);

		virtual ~TraceContainerCallContext();

		mdTypeDef GetType() const { return m_typeDef; }

		mdMethodDef GetCurrentMethod() const { return m_getCurrentMethodDef; }

		mdFieldDef GetContextIdHighField() const;
		mdFieldDef GetContextIdLowField() const;

	private:
		bool ShouldRegisterType(const ModuleID moduleId) const override;
		HRESULT RegisterType(const ModuleID moduleId) override;
		
		HRESULT InjectTypeImplementation(ModuleID moduleId) override;

		HRESULT RegisterImplementationTypeDependencies(const ModuleID moduleId, ATL::CComPtr<IMetaDataImport>& metaDataImport);

		HRESULT InjectStaticCtorImplementation(const ModuleID moduleId) const;
		HRESULT InjectCtorImplementation(const ModuleID moduleId) const;
		HRESULT InjectGetCurrentImplementation(const ModuleID moduleId) const;

		std::shared_ptr<TraceContainerBase> m_traceContainerBase;

		mdTypeDef m_typeDef;
		mdMethodDef m_ctorDef;
		mdMethodDef m_cctorDef;
		mdFieldDef m_traceContainerKeyFieldDef;
		mdMethodDef m_getCurrentMethodDef;
		mdString m_traceContainerString;

		mdSignature m_getCurrentLocalVariablesSignature;

		mdMethodDef m_callContextLogicalGetDataMethodDef;
		mdMethodDef m_callContextLogicalSetDataMethodDef;
	};
}