// Copyright 2017 Secure Decisions, a division of Applied Visions, Inc. 
// Permission is hereby granted, free of charge, to any person obtaining a copy of 
// this software and associated documentation files (the "Software"), to deal in the 
// Software without restriction, including without limitation the rights to use, copy, 
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the 
// following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies 
// or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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

		mdToken m_addBeginRequestDef;
		mdToken m_addEndRequestDef;

		mdMemberRef m_traceContainerGetCurrentRef;
		
		mdMemberRef m_eventHandlerCtorRef;
		
		mdMemberRef m_traceContainerBaseContextIdRef;
		mdMemberRef m_traceContainerBaseSetContextIdRef;
		mdMemberRef m_traceContainerBaseNotifyContextEndRef;

		mdTypeDef m_httpApplicationTypeDef;
		mdToken m_httpApplicationGetContext;

		mdToken m_httpContextGetItems;

		mdTypeRef m_guidTypeRef;
		mdMemberRef m_guidParseRef;

		mdSignature m_endRequestLocalVariablesSignature;

		mdString m_contextIdKey;

		mdMemberRef m_objectToStringRef;

		mdMemberRef m_dictionarySetItem;
		mdMemberRef m_dictionaryGetItem;
	};
}
