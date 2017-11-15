#pragma once

#include <cor.h>
#include <corprof.h>
#include <gmock/gmock.h>

class MockICorProfilerInfo : public ICorProfilerInfo
{
public:
	virtual ~MockICorProfilerInfo() {}

	MOCK_METHOD2_WITH_CALLTYPE(__stdcall, QueryInterface, HRESULT(const IID& riid, void** ppvObject));

	MOCK_METHOD0_WITH_CALLTYPE(__stdcall, AddRef, ULONG(void));
	MOCK_METHOD0_WITH_CALLTYPE(__stdcall, Release, ULONG(void));

	MOCK_METHOD2_WITH_CALLTYPE(__stdcall, GetClassFromObject,
		HRESULT(ObjectID objectId, /* [out] */ ClassID *pClassId));
	MOCK_METHOD3_WITH_CALLTYPE(__stdcall, GetClassFromToken,
		HRESULT(ModuleID moduleId, /* [in] */ mdTypeDef typeDef, /* [out] */ ClassID *pClassId));
	MOCK_METHOD3_WITH_CALLTYPE(__stdcall, GetCodeInfo,
		HRESULT(FunctionID functionId, /* [out] */ LPCBYTE *pStart, /* [out] */ ULONG *pcSize));
	MOCK_METHOD1_WITH_CALLTYPE(__stdcall, GetEventMask,
		HRESULT(DWORD *pdwEvents));
	MOCK_METHOD2_WITH_CALLTYPE(__stdcall, GetFunctionFromIP,
		HRESULT(LPCBYTE ip, /* [out] */ FunctionID *pFunctionId));
	MOCK_METHOD3_WITH_CALLTYPE(__stdcall, GetFunctionFromToken,
		HRESULT(ModuleID moduleId, /* [in] */ mdToken token, /* [out] */ FunctionID *pFunctionId));
	MOCK_METHOD2_WITH_CALLTYPE(__stdcall, GetHandleFromThread,
		HRESULT(ThreadID threadId, /* [out] */ HANDLE *phThread));
	MOCK_METHOD2_WITH_CALLTYPE(__stdcall, GetObjectSize,
		HRESULT(ObjectID objectId, /* [out] */ ULONG *pcSize));
	MOCK_METHOD4_WITH_CALLTYPE(__stdcall, IsArrayClass,
		HRESULT(ClassID classId, /* [out] */ CorElementType *pBaseElemType, /* [out] */ ClassID *pBaseClassId, /* [out] */ ULONG *pcRank));
	MOCK_METHOD2_WITH_CALLTYPE(__stdcall, GetThreadInfo,
		HRESULT(ThreadID threadId, /* [out] */ DWORD *pdwWin32ThreadId));
	MOCK_METHOD1_WITH_CALLTYPE(__stdcall, GetCurrentThreadID,
		HRESULT(ThreadID *pThreadId));
	MOCK_METHOD3_WITH_CALLTYPE(__stdcall, GetClassIDInfo,
		HRESULT(ClassID classId, /* [out] */ ModuleID *pModuleId, /* [out] */ mdTypeDef *pTypeDefToken));
	MOCK_METHOD4_WITH_CALLTYPE(__stdcall, GetFunctionInfo,
		HRESULT(FunctionID functionId, /* [out] */ ClassID *pClassId, /* [out] */ ModuleID *pModuleId, /* [out] */ mdToken *pToken));
	MOCK_METHOD1_WITH_CALLTYPE(__stdcall, SetEventMask,
		HRESULT(DWORD dwEvents));
	MOCK_METHOD3_WITH_CALLTYPE(__stdcall, SetEnterLeaveFunctionHooks,
		HRESULT(FunctionEnter *pFuncEnter, /* [in] */ FunctionLeave *pFuncLeave, /* [in] */ FunctionTailcall *pFuncTailcall));
	MOCK_METHOD1_WITH_CALLTYPE(__stdcall, SetFunctionIDMapper,
		HRESULT(FunctionIDMapper *pFunc));
	MOCK_METHOD4_WITH_CALLTYPE(__stdcall, GetTokenAndMetaDataFromFunction,
		HRESULT(FunctionID functionId, /* [in] */ REFIID riid, /* [out] */ IUnknown **ppImport, /* [out] */ mdToken *pToken));
	MOCK_METHOD6_WITH_CALLTYPE(__stdcall, GetModuleInfo,
		HRESULT(ModuleID moduleId, /* [out] */ LPCBYTE *ppBaseLoadAddress, /* [in] */ ULONG cchName, /* [out] */ ULONG *pcchName, /* [annotation][out] _Out_writes_to_(cchName, *pcchName) */ WCHAR szName[], /* [out] */ AssemblyID *pAssemblyId));
	MOCK_METHOD4_WITH_CALLTYPE(__stdcall, GetModuleMetaData,
		HRESULT(ModuleID moduleId, /* [in] */ DWORD dwOpenFlags, /* [in] */ REFIID riid, /* [out] */ IUnknown **ppOut));
	MOCK_METHOD4_WITH_CALLTYPE(__stdcall, GetILFunctionBody,
		HRESULT(ModuleID moduleId, /* [in] */ mdMethodDef methodId, /* [out] */ LPCBYTE *ppMethodHeader, /* [out] */ ULONG *pcbMethodSize));
	MOCK_METHOD2_WITH_CALLTYPE(__stdcall, GetILFunctionBodyAllocator,
		HRESULT(ModuleID moduleId, /* [out] */ IMethodMalloc **ppMalloc));
	MOCK_METHOD3_WITH_CALLTYPE(__stdcall, SetILFunctionBody,
		HRESULT(ModuleID moduleId, /* [in] */ mdMethodDef methodid, /* [in] */ LPCBYTE pbNewILMethodHeader));
	MOCK_METHOD5_WITH_CALLTYPE(__stdcall, GetAppDomainInfo,
		HRESULT(AppDomainID appDomainId, /* [in] */ ULONG cchName, /* [out] */ ULONG *pcchName, /* [annotation][out] _Out_writes_to_(cchName, *pcchName) */ WCHAR szName[], /* [out] */ ProcessID *pProcessId));
	MOCK_METHOD6_WITH_CALLTYPE(__stdcall, GetAssemblyInfo,
		HRESULT(AssemblyID assemblyId, /* [in] */ ULONG cchName, /* [out] */ ULONG *pcchName, /* [annotation][out] _Out_writes_to_(cchName, *pcchName) */ WCHAR szName[], /* [out] */ AppDomainID *pAppDomainId, /* [out] */ ModuleID *pModuleId));
	MOCK_METHOD1_WITH_CALLTYPE(__stdcall, SetFunctionReJIT,
		HRESULT(FunctionID functionId));
	MOCK_METHOD0_WITH_CALLTYPE(__stdcall, ForceGC,
		HRESULT(void));
	MOCK_METHOD4_WITH_CALLTYPE(__stdcall, SetILInstrumentedCodeMap,
		HRESULT(FunctionID functionId, /* [in] */ BOOL fStartJit, /* [in] */ ULONG cILMapEntries, /* [size_is][in] */ COR_IL_MAP rgILMapEntries[]));
	MOCK_METHOD1_WITH_CALLTYPE(__stdcall, GetInprocInspectionInterface,
		HRESULT(IUnknown **ppicd));
	MOCK_METHOD1_WITH_CALLTYPE(__stdcall, GetInprocInspectionIThisThread,
		HRESULT(IUnknown **ppicd));
	MOCK_METHOD2_WITH_CALLTYPE(__stdcall, GetThreadContext,
		HRESULT(ThreadID threadId, /* [out] */ ContextID *pContextId));
	MOCK_METHOD2_WITH_CALLTYPE(__stdcall, BeginInprocDebugging,
		HRESULT(BOOL fThisThreadOnly, /* [out] */ DWORD *pdwProfilerContext));
	MOCK_METHOD1_WITH_CALLTYPE(__stdcall, EndInprocDebugging,
		HRESULT(DWORD dwProfilerContext));
	MOCK_METHOD4_WITH_CALLTYPE(__stdcall, GetILToNativeMapping,
		HRESULT(FunctionID functionId, /* [in] */ ULONG32 cMap, /* [out] */ ULONG32 *pcMap, /* [length_is][size_is][out] */ COR_DEBUG_IL_TO_NATIVE_MAP map[]));
};