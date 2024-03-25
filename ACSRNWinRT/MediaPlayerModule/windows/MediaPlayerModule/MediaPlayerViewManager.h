#pragma once

#include "NativeModules.h"
#include "winrt/Microsoft.ReactNative.h"

namespace winrt::MediaPlayerModule::implementation
{
    class MediaPlayerViewManager : public winrt::implements<
        MediaPlayerViewManager,
        winrt::Microsoft::ReactNative::IViewManager,
        winrt::Microsoft::ReactNative::IViewManagerWithReactContext,
        winrt::Microsoft::ReactNative::IViewManagerWithNativeProperties,
        winrt::Microsoft::ReactNative::IViewManagerWithExportedEventTypeConstants,
        winrt::Microsoft::ReactNative::IViewManagerRequiresNativeLayout>
    {
    public:
        MediaPlayerViewManager();

        // IViewManager
        winrt::hstring Name() noexcept;
        winrt::Windows::UI::Xaml::FrameworkElement CreateView() noexcept;

        // IViewManagerWithReactContext
        winrt::Microsoft::ReactNative::IReactContext ReactContext() noexcept;
        void ReactContext(winrt::Microsoft::ReactNative::IReactContext reactContext) noexcept;

        // IViewManagerWithNativeProperties
        winrt::Windows::Foundation::Collections::IMapView<winrt::hstring,
            winrt::Microsoft::ReactNative::ViewManagerPropertyType>
        NativeProps() noexcept;

        // IViewManagerWithExportedEventTypeConstants
        winrt::Microsoft::ReactNative::ConstantProviderDelegate ExportedCustomBubblingEventTypeConstants() noexcept;
        winrt::Microsoft::ReactNative::ConstantProviderDelegate ExportedCustomDirectEventTypeConstants() noexcept;

        // IViewManagerRequiresNativeLayout
        bool RequiresNativeLayout()
        {
            return true;
        }

        void UpdateProperties(winrt::Windows::UI::Xaml::FrameworkElement const& view,
            winrt::Microsoft::ReactNative::IJSValueReader const& propertyMapReader) noexcept;

    private:
        winrt::Microsoft::ReactNative::IReactContext m_reactContext{ nullptr };
    };
}