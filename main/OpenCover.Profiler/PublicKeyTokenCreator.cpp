#include "stdafx.h"

#include "PublicKeyTokenCreator.h"

namespace Injection
{
	PublicKeyTokenCreator::PublicKeyTokenCreator()
	{
		m_hCryptProv = 0;

		if (!CryptAcquireContext(
			&m_hCryptProv,
			nullptr,
			nullptr,
			PROV_RSA_FULL,
			CRYPT_VERIFYCONTEXT))
		{
			m_hCryptProv = 0;
			return;
		}
	}

	PublicKeyTokenCreator::~PublicKeyTokenCreator()
	{
		if (m_hCryptProv == 0)
		{
			return;
		}
		CryptReleaseContext(m_hCryptProv, 0);
	}

	bool PublicKeyTokenCreator::GetPublicKeyToken(BYTE* pbPublicKey, const DWORD cbPublicKey, const ALG_ID hashAlgorithmId,
		std::vector<BYTE>& publicKeyToken) const
	{
		if (m_hCryptProv == 0)
		{
			return false;
		}

		if (pbPublicKey == nullptr)
		{
			return false;
		}
		publicKeyToken.clear();

		HCRYPTHASH hHash;
		if (!CryptCreateHash(
			m_hCryptProv,
			hashAlgorithmId,
			0,
			0,
			&hHash))
		{
			return false;
		}

		if (!CryptHashData(hHash, pbPublicKey, cbPublicKey, 0))
		{
			return false;
		}

		const auto hashBufferSize = 150;
		BYTE hashBuffer[hashBufferSize];
		DWORD cbHashBuffer = hashBufferSize;

		const auto result = CryptGetHashParam(hHash,
			HP_HASHVAL,
			hashBuffer,
			&cbHashBuffer, 0);

		if (result && cbHashBuffer >= 8)
		{
			const auto lastIndex = cbHashBuffer - 8;
			for (auto index = cbHashBuffer - 1; index >= lastIndex; index--)
			{
				publicKeyToken.push_back(hashBuffer[index]);
			}
		}

		CryptDestroyHash(hHash);
		return result;
	}
}