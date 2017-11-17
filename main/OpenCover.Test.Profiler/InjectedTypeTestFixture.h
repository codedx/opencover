#pragma once

#include "stdafx.h"

#include "InjectedType.h"
#include "AssemblyRegistry.h"
#include "MockICorProfilerInfo.h"
#include "MockIMetaDataAssemblyImport.h"
#include "MockIMetaDataAssemblyEmit.h"

class InjectedTypeTestFixture : public testing::Test 
{
protected:
	InjectedTypeTestFixture() :
		Test(),
		profilerInfoPtr(&profilerInfo),
		assemblyRegistry(std::make_shared<Injection::AssemblyRegistry>(profilerInfoPtr))
	{
	}

	void SetUp() override
	{
		EXPECT_CALL(profilerInfo, GetModuleMetaData(testing::_, testing::_, IID_IMetaDataAssemblyImport, testing::_))
			.WillRepeatedly(DoAll(testing::SetArgPointee<3>(&metaDataAssemblyImport), testing::Return(S_OK)));
		EXPECT_CALL(profilerInfo, GetModuleMetaData(testing::_, testing::_, IID_IMetaDataAssemblyEmit, testing::_))
			.WillRepeatedly(DoAll(testing::SetArgPointee<3>(&metaDataAssemblyEmit), testing::Return(S_OK)));
	}

	void TearDown() override
	{
	}

	MockICorProfilerInfo profilerInfo;
	CComPtr<ICorProfilerInfo> profilerInfoPtr;

	std::shared_ptr<Injection::AssemblyRegistry> assemblyRegistry;

	MockIMetaDataAssemblyImport metaDataAssemblyImport;
	MockIMetaDataAssemblyEmit metaDataAssemblyEmit;
};
