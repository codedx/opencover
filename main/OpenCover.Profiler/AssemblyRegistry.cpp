#include "stdafx.h"
#include <sstream>
#include <algorithm>

#include "AssemblyRegistry.h"
#include "PublicKeyTokenCreator.h"

namespace Injection
{
	AssemblyRegistry::AssemblyRegistry(const ATL::CComPtr<ICorProfilerInfo>& profilerInfo)
		: m_profilerInfo(profilerInfo)
	{
	}

	bool AssemblyRegistry::FillAssembliesByName(const std::wstring& name, std::vector<AssemblyReference>& referencedAssemblies) const
	{
		const auto knownAssemblyReferences = m_assemblyVersionRegistry.find(name);
		if (knownAssemblyReferences == m_assemblyVersionRegistry.end())
		{
			return false;
		}

		auto iter = knownAssemblyReferences->second.begin();
		while (iter != knownAssemblyReferences->second.end())
		{
			referencedAssemblies.push_back(*iter);
			++iter;
		}
		return referencedAssemblies.size() > 0;
	}

	bool AssemblyRegistry::FindMaxAssemblyVersion(const std::wstring& name, AssemblyVersion& assemblyVersion) const
	{
		std::vector<AssemblyReference> referencedAssemblies;
		if (!FillAssembliesByName(name, referencedAssemblies))
		{
			return false;
		}

		std::vector<AssemblyVersion> assemblyVersions;
		for (auto iter = referencedAssemblies.begin(); iter != referencedAssemblies.end(); ++iter)
		{
			assemblyVersions.push_back(iter->version);
		}

		const auto maxElement = max_element(assemblyVersions.begin(), assemblyVersions.end());

		assemblyVersion = *maxElement;
		return true;
	}

	HRESULT AssemblyRegistry::RecordAssemblyMetadataForModule(const ModuleID moduleId)
	{
		ATL::CComPtr<IMetaDataAssemblyImport> metaDataAssemblyImport;
		HRESULT result = m_profilerInfo->GetModuleMetaData(moduleId,
			ofRead | ofWrite, IID_IMetaDataAssemblyImport, reinterpret_cast<IUnknown**>(&metaDataAssemblyImport));

		if (!SUCCEEDED(result))
		{
			return result;
		}

		if (metaDataAssemblyImport == nullptr) return E_FAIL;

		unsigned char* pbPublicKeyOrToken;
		ULONG cbPublicKeyOrToken = 0;
		ULONG hashAlgId = 0;
		const ULONG chName = 250;
		WCHAR zName[chName];
		ULONG chNameOut = 0;
		ASSEMBLYMETADATA metaData;
		DWORD assemblyRefFlags = 0;

		ZeroMemory(zName, sizeof(WCHAR) * 250);
		ZeroMemory(&metaData, sizeof(ASSEMBLYMETADATA));

		auto assembly = mdAssemblyNil;
		result = metaDataAssemblyImport->GetAssemblyFromScope(&assembly);
		if (!SUCCEEDED(result))
		{
			return result;
		}

		result = metaDataAssemblyImport->GetAssemblyProps(assembly,
			(const void**)&pbPublicKeyOrToken,
			&cbPublicKeyOrToken,
			&hashAlgId,
			zName,
			chName,
			&chNameOut,
			&metaData,
			&assemblyRefFlags);

		if (!SUCCEEDED(result))
		{
			return result;
		}

		StoreAssemblyDetails(pbPublicKeyOrToken, cbPublicKeyOrToken, hashAlgId, zName, chNameOut, metaData, assemblyRefFlags);

		HCORENUM hEnum = nullptr;
		mdAssemblyRef references[20];

		result = S_OK;
		while (result == S_OK)
		{
			ZeroMemory(zName, sizeof(WCHAR) * 250);
			ZeroMemory(&metaData, sizeof(ASSEMBLYMETADATA));
			ZeroMemory(references, 20 * sizeof(mdAssemblyRef));

			ULONG refCount = 0;
			result = metaDataAssemblyImport->EnumAssemblyRefs(&hEnum,
				references,
				20,
				&refCount);

			if (!SUCCEEDED(result)) continue;

			for (auto i = 0ul; i < refCount; i++)
			{
				const auto propsResult = metaDataAssemblyImport->GetAssemblyRefProps(references[i],
					(const void**)&pbPublicKeyOrToken,
					&cbPublicKeyOrToken,
					zName,
					chName,
					&chNameOut,
					&metaData,
					nullptr,
					nullptr,
					&assemblyRefFlags);

				if (propsResult != S_OK)
				{
					metaDataAssemblyImport->CloseEnum(hEnum);
					return propsResult;
				}

				StoreAssemblyDetails(pbPublicKeyOrToken, cbPublicKeyOrToken, CALG_SHA1, zName, chNameOut, metaData, assemblyRefFlags);
			}
		}
		metaDataAssemblyImport->CloseEnum(hEnum);

		return S_OK;
	}

	bool AssemblyRegistry::StoreAssemblyDetails(unsigned char* pbPublicKeyOrToken,
		const ULONG cbPublicKeyOrToken,
		const ALG_ID hashAlgId,
		wchar_t* pzName,
		const ULONG chNameOut,
		const ASSEMBLYMETADATA& metaData,
		const DWORD flags)
	{
		std::wstringstream assemblyNameStream;
		for (auto j = 0ul; j < chNameOut; j++)
		{
			if (pzName[j] == NULL)
				break;

			assemblyNameStream << pzName[j];
		}

		AssemblyReference assemblyReference;
		assemblyReference.version.majorVersion = metaData.usMajorVersion;
		assemblyReference.version.minorVersion = metaData.usMinorVersion;
		assemblyReference.version.buildNumber = metaData.usBuildNumber;
		assemblyReference.version.revisionNumber = metaData.usRevisionNumber;
		assemblyReference.name = std::wstring(assemblyNameStream.str());

		ZeroMemory(assemblyReference.publicKeyToken, sizeof(assemblyReference.publicKeyToken));

		const auto sizeOfPublicKeyToken = 8ul;
		if (cbPublicKeyOrToken == sizeOfPublicKeyToken)
		{
			memcpy(assemblyReference.publicKeyToken, pbPublicKeyOrToken, cbPublicKeyOrToken);
		}
		else
		{
			std::vector<BYTE> publicKeyToken;
			PublicKeyTokenCreator tokenCreator;
			if (tokenCreator.GetPublicKeyToken(pbPublicKeyOrToken, cbPublicKeyOrToken, hashAlgId, publicKeyToken))
			{
				copy(publicKeyToken.begin(), publicKeyToken.end(), assemblyReference.publicKeyToken);
			}
		}

		auto knownAssemblyReference = m_assemblyVersionRegistry.find(assemblyReference.name);
		if (knownAssemblyReference == m_assemblyVersionRegistry.end())
		{
			std::vector<AssemblyReference> assemblyVersions;
			assemblyVersions.push_back(assemblyReference);
			m_assemblyVersionRegistry.insert(make_pair(assemblyReference.name, assemblyVersions));
			return true;
		}

		const auto knownAssemblyVersion = find(knownAssemblyReference->second.begin(), knownAssemblyReference->second.end(), assemblyReference);
		if (knownAssemblyVersion == knownAssemblyReference->second.end())
		{
			knownAssemblyReference->second.push_back(assemblyReference);
		}
		return false;
	}
}