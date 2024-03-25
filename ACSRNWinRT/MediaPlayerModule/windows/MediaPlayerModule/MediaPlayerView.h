#include "Generated Files/MediaPlayerView.g.h"
#include "NativeModules.h"

namespace winrt::MediaPlayerModule::implementation
{
    namespace xaml = winrt::Windows::UI::Xaml;

    class MediaPlayerView : public MediaPlayerViewT<MediaPlayerView>
    {
    public:
        MediaPlayerView(Microsoft::ReactNative::IReactContext const& reactContext);
        void UpdateProperties(Microsoft::ReactNative::IJSValueReader const& reader);

    private:
        Microsoft::ReactNative::IReactContext m_reactContext{ nullptr };
        bool m_updating{ false };
        void RegisterEvents();
    };
}

namespace winrt::MediaPlayerModule::factory_implementation
{
    struct MediaPlayerView : MediaPlayerViewT<MediaPlayerView, implementation::MediaPlayerView>
    {
    };
}