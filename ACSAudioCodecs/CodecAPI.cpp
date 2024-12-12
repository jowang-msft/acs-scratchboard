//#include "pch.h"

#include "CodecAPI.h"

#include <iostream>
#include <math.h>
#include <random>
#include <stdint.h>
#include <stdlib.h>
#include <vector>
#include <fstream>

#include <AudioPayloadEnum.h>
#include <spl/spl_to_string.hpp>
#include <spl/spl_wrap.hpp>
#include "audiocodecs_api.h"
#include <opus_api.h>

#define MAX_OUT_BUFFER_SIZE 1000000;

void Encode(
    unsigned char* inputBuffer,
    int length
)
{

}

void Decode(
    unsigned char* inputBuffer,
    int length)
{
    using namespace audiocodecs;

    AESettingsSend aeSettingsSend = {};
    AESettingsRecv aeSettingsRecv = {};

    AUDIO_ENCODER_CLASS* pEncoder = nullptr;
    AUDIO_DECODER_CLASS* pDecoder = nullptr;
    HRESULT hr = AC_S_OK;
    AudioPayloadType payloadType = AudioPayloadType::OPUS;
    aeSettingsSend.enableAnchorEncoder = true;

    if (AudioPayloadType::OPUS == payloadType) {
        hr = audiocodecs::CAudioDecode_OPUS_c::CreateInstance(&pDecoder, payloadType, aeSettingsRecv);
        hr = pDecoder->DecodeInit();

        size_t bufferSize = MAX_OUT_BUFFER_SIZE;

        unsigned char* outBuffer = new unsigned char[bufferSize];
        for (size_t i = 0; i < bufferSize; i++)
        {
            outBuffer[i] = 0;
        }

        int32_t actualUsedSize = 0;
        int32_t destSize = bufferSize;
        hr = pDecoder->Decode(inputBuffer, length, outBuffer, &destSize, 2, &actualUsedSize);

        delete[] outBuffer;
    }
}