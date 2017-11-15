#pragma once

namespace Injection
{
	class PublicKeyTokenCreator
	{
	public:
		PublicKeyTokenCreator();
		~PublicKeyTokenCreator();

		bool GetPublicKeyToken(BYTE* pbPublicKey, const DWORD cbPublicKey, const ALG_ID hashAlgorithmId,
			std::vector<BYTE>& publicKeyToken) const;

	private:
		HCRYPTPROV m_hCryptProv;
	};
}
