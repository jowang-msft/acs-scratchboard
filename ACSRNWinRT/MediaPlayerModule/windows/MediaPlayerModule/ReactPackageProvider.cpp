// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include "pch.h"
#include "ReactPackageProvider.h"
#if __has_include("ReactPackageProvider.g.cpp")
#include "ReactPackageProvider.g.cpp"
#endif

// NOTE: You must include the headers of your native modules here in
// order for the AddAttributedModules call below to find them.

#include "MediaPlayerViewManager.h"

using namespace winrt::Microsoft::ReactNative;

namespace winrt::MediaPlayerModule::implementation
{

void ReactPackageProvider::CreatePackage(IReactPackageBuilder const &packageBuilder) noexcept
{
    AddAttributedModules(packageBuilder, true);
    packageBuilder.AddViewManager(L"MediaPlayerViewManager",
        []() { return winrt::make<winrt::MediaPlayerModule::implementation::MediaPlayerViewManager>(); });
}

} // namespace winrt::MediaPlayerModule::implementation
