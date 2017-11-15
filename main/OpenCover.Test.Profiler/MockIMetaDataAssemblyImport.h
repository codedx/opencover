#pragma once

#include <cor.h>
#include <gmock/gmock.h>

class MockIMetaDataAssemblyImport : public IMetaDataAssemblyImport {
public:
	MOCK_METHOD2_WITH_CALLTYPE(__stdcall, QueryInterface, HRESULT(const IID& riid, void** ppvObject));

	MOCK_METHOD0_WITH_CALLTYPE(__stdcall, AddRef, ULONG(void));
	MOCK_METHOD0_WITH_CALLTYPE(__stdcall, Release, ULONG(void));

	MOCK_METHOD9_WITH_CALLTYPE(__stdcall, GetAssemblyProps,
		HRESULT(mdAssembly mda, const void **ppbPublicKey, ULONG *pcbPublicKey, ULONG *pulHashAlgId, LPWSTR szName, ULONG cchName, ULONG *pchName, ASSEMBLYMETADATA *pMetaData, DWORD *pdwAssemblyFlags));
	MOCK_METHOD10_WITH_CALLTYPE(__stdcall, GetAssemblyRefProps,
		HRESULT(mdAssemblyRef mdar, const void **ppbPublicKeyOrToken, ULONG *pcbPublicKeyOrToken, LPWSTR szName, ULONG cchName, ULONG *pchName, ASSEMBLYMETADATA *pMetaData, const void **ppbHashValue, ULONG *pcbHashValue, DWORD *pdwAssemblyRefFlags));
	MOCK_METHOD7_WITH_CALLTYPE(__stdcall, GetFileProps,
		HRESULT(mdFile mdf, LPWSTR szName, ULONG cchName, ULONG *pchName, const void **ppbHashValue, ULONG *pcbHashValue, DWORD *pdwFileFlags));
	MOCK_METHOD7_WITH_CALLTYPE(__stdcall, GetExportedTypeProps,
		HRESULT(mdExportedType mdct, LPWSTR szName, ULONG cchName, ULONG *pchName, mdToken *ptkImplementation, mdTypeDef *ptkTypeDef, DWORD *pdwExportedTypeFlags));
	MOCK_METHOD7_WITH_CALLTYPE(__stdcall, GetManifestResourceProps,
		HRESULT(mdManifestResource mdmr, LPWSTR szName, ULONG cchName, ULONG *pchName, mdToken *ptkImplementation, DWORD *pdwOffset, DWORD *pdwResourceFlags));
	MOCK_METHOD4_WITH_CALLTYPE(__stdcall, EnumAssemblyRefs,
		HRESULT(HCORENUM *phEnum, mdAssemblyRef rAssemblyRefs[], ULONG cMax, ULONG *pcTokens));
	MOCK_METHOD4_WITH_CALLTYPE(__stdcall, EnumFiles,
		HRESULT(HCORENUM *phEnum, mdFile rFiles[], ULONG cMax, ULONG *pcTokens));
	MOCK_METHOD4_WITH_CALLTYPE(__stdcall, EnumExportedTypes,
		HRESULT(HCORENUM *phEnum, mdExportedType rExportedTypes[], ULONG cMax, ULONG *pcTokens));
	MOCK_METHOD4_WITH_CALLTYPE(__stdcall, EnumManifestResources,
		HRESULT(HCORENUM *phEnum, mdManifestResource rManifestResources[], ULONG cMax, ULONG *pcTokens));
	MOCK_METHOD1_WITH_CALLTYPE(__stdcall, GetAssemblyFromScope,
		HRESULT(mdAssembly *ptkAssembly));
	MOCK_METHOD3_WITH_CALLTYPE(__stdcall, FindExportedTypeByName,
		HRESULT(LPCWSTR szName, mdToken mdtExportedType, mdExportedType *ptkExportedType));
	MOCK_METHOD2_WITH_CALLTYPE(__stdcall, FindManifestResourceByName,
		HRESULT(LPCWSTR szName, mdManifestResource *ptkManifestResource));
	MOCK_METHOD1_WITH_CALLTYPE(__stdcall, CloseEnum,
		void (HCORENUM hEnum));
	MOCK_METHOD6_WITH_CALLTYPE(__stdcall, FindAssembliesByName,
		HRESULT(LPCWSTR szAppBase, LPCWSTR szPrivateBin, LPCWSTR szAssemblyName, IUnknown *ppIUnk[], ULONG cMax, ULONG *pcAssemblies));
};