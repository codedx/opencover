#pragma once

#include "stdafx.h"

#include "InjectedType.h"
#include "AssemblyRegistry.h"
#include "MockICorProfilerInfo.h"

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
	}

	void TearDown() override
	{
	}

	MockICorProfilerInfo profilerInfo;
	CComPtr<ICorProfilerInfo> profilerInfoPtr;

	std::shared_ptr<Injection::AssemblyRegistry> assemblyRegistry;
};