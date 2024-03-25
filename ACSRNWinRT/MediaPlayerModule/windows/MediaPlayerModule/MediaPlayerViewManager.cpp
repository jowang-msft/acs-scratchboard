#include "pch.h"
#include "MediaPlayerViewManager.h"
#include "MediaPlayerView.h"
#include "NativeModules.h"

namespace winrt
{
    using namespace Microsoft::ReactNative;
    using namespace Windows::Foundation::Collections;

    namespace xaml = winrt::Windows::UI::Xaml;
}

namespace winrt::MediaPlayerModule::implementation
{
    MediaPlayerViewManager::MediaPlayerViewManager()
    {
    }

    // IViewManager
    winrt::hstring MediaPlayerViewManager::Name() noexcept
    {
        return L"MediaPlayerView";
    }

    xaml::FrameworkElement MediaPlayerViewManager::CreateView() noexcept
    {
        return winrt::MediaPlayerModule::MediaPlayerView(m_reactContext);
    }

    // IViewManagerWithReactContext
    winrt::IReactContext MediaPlayerViewManager::ReactContext() noexcept
    {
        return m_reactContext;
    }

    void MediaPlayerViewManager::ReactContext(IReactContext reactContext) noexcept
    {
        m_reactContext = reactContext;
    }

    // IViewManagerWithNativeProperties
    IMapView<hstring, ViewManagerPropertyType> MediaPlayerViewManager::NativeProps() noexcept
    {
        auto nativeProps = winrt::single_threaded_map<hstring, ViewManagerPropertyType>();

        nativeProps.Insert(L"source", ViewManagerPropertyType::String);

        return nativeProps.GetView();
    }

    void MediaPlayerViewManager::UpdateProperties(
        xaml::FrameworkElement const& view, IJSValueReader const& propertyMapReader) noexcept
    {
        if (auto mediaPlayerView = view.try_as<MediaPlayerView>())
        {
            mediaPlayerView->UpdateProperties(propertyMapReader);
        }
        else
        {
            OutputDebugStringW(L"Type deduction for MediaPlayerView failed.");
        }
    }

    // IViewManagerWithExportedEventTypeConstants
    ConstantProviderDelegate MediaPlayerViewManager::ExportedCustomBubblingEventTypeConstants() noexcept
    {
        return nullptr;
    }

    ConstantProviderDelegate MediaPlayerViewManager::ExportedCustomDirectEventTypeConstants() noexcept
    {
        return nullptr;
    }
}