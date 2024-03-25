#include "pch.h"
#include "MediaPlayerView.h"
#include "Generated Files/MediaPlayerView.g.cpp"
#include "JSValueXaml.h"
#include <winrt/Windows.Media.Core.h>
#include <winrt/Windows.Media.Playback.h>

namespace winrt
{
    using namespace Microsoft::ReactNative;
    using namespace Windows::Foundation;
}

namespace winrt::MediaPlayerModule::implementation
{
    MediaPlayerView::MediaPlayerView(winrt::IReactContext const& reactContext) : m_reactContext(reactContext)
    {
        RegisterEvents();
    }

    void MediaPlayerView::RegisterEvents()
    {
        // TODO:Register events.
    }

    void MediaPlayerView::UpdateProperties(winrt::IJSValueReader const& reader)
    {
        m_updating = true;

        bool updateSelectedDate = false;
        bool updateMaxDate = false;
        bool updateMinDate = false;

        auto const& propertyMap = JSValueObject::ReadFrom(reader);

        for (auto const& pair : propertyMap)
        {
            auto const& propertyName = pair.first;
            auto const& propertyValue = pair.second;

            if (propertyName == "source")
            {
                if (propertyValue.IsNull())
                {
                    this->ClearValue(
                        xaml::Controls::MediaPlayerElement::SourceProperty());
                }
                else
                {
                    auto source = propertyValue.AsString();
                    auto src = winrt::Windows::Media::Core::MediaSource::CreateFromUri(
                        winrt::Windows::Foundation::Uri(winrt::to_hstring(propertyValue.AsString())));
                    this->Source(src);
                    this->AutoPlay(true); // TODO: Leave tihs to the caller.
                }
            }
        }

        m_updating = false;
        return;
    }
}