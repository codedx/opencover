#pragma once
#include "InjectedType.h"

namespace Context
{
	class TraceContainerBase :
		public Injection::InjectedType
	{
	public:
		TraceContainerBase(const ATL::CComPtr<ICorProfilerInfo>& profilerInfo,
			const std::shared_ptr<Injection::AssemblyRegistry>& assemblyRegistry);

		virtual ~TraceContainerBase();

		mdTypeDef GetType() const { return m_typeDef; }

		mdMethodDef GetCtorMethod() const { return m_ctorMethodDef; }

		mdFieldDef GetContextIdHighField() const { return m_contextIdHighFieldDef; }
		mdFieldDef GetContextIdLowField() const { return m_contextIdLowFieldDef; }

	private:
		bool ShouldRegisterType(const ModuleID moduleId) const override;
		HRESULT RegisterType(const ModuleID moduleId) override;

		HRESULT InjectTypeImplementation(ModuleID moduleId) override;

		HRESULT RegisterImplementationTypeDependencies(const ModuleID moduleId, ATL::CComPtr<IMetaDataImport>& metaDataImport);

		HRESULT InjectCtorImplementation(const ModuleID moduleId, ATL::CComPtr<IMetaDataEmit>& metaDataEmit) const;

		mdTypeDef m_typeDef;
		mdMethodDef m_ctorMethodDef;

		mdFieldDef m_contextIdHighFieldDef;
		mdFieldDef m_contextIdLowFieldDef;
		mdFieldDef m_contextIdFieldDef;


		mdTypeDef m_systemObject;
		mdMethodDef m_systemObjectCtor;

		mdTypeDef m_guidTypeDef;
		mdMethodDef m_guidNewGuidMethodDef;
		mdMethodDef m_guidToByteArrayMethodDef;

		mdMethodDef m_bitConverterToUInt64MethodDef;

		mdSignature m_ctorLocalVariablesSignature;
	};
}
