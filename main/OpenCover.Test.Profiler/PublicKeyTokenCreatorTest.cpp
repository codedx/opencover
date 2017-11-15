#include "stdafx.h"

#include "..\OpenCover.Profiler\PublicKeyTokenCreator.h"

using namespace std;
using namespace Injection;

TEST(PublicKeyTokenCreatorTest, CanGeneratorPublicKeyToken)
{
	const DWORD cbPublicKey = 16;
	BYTE publicKey[] = { 00, 00, 00, 00, 00, 00, 00, 00, 04, 00, 00, 00, 00, 00, 00, 00 };

	vector<BYTE> publicKeyToken;

	PublicKeyTokenCreator publicKeyTokenCreator;
	auto result = publicKeyTokenCreator.GetPublicKeyToken(publicKey, cbPublicKey, CALG_SHA1, publicKeyToken);

	ASSERT_TRUE(result);
	ASSERT_EQ(0xB7, publicKeyToken[0]);
	ASSERT_EQ(0x7A, publicKeyToken[1]);
	ASSERT_EQ(0x5C, publicKeyToken[2]);
	ASSERT_EQ(0x56, publicKeyToken[3]);
	ASSERT_EQ(0x19, publicKeyToken[4]);
	ASSERT_EQ(0x34, publicKeyToken[5]);
	ASSERT_EQ(0xE0, publicKeyToken[6]);
	ASSERT_EQ(0x89, publicKeyToken[7]);
}