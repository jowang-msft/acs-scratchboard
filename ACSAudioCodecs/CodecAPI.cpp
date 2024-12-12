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

#include <AudioHealerAPI.h>
#include "CoreAudioHealerDefines.h"

#include "AudioDecoderAPI.h"

#define MAX_OUT_BUFFER_SIZE 1000000;

void Encode(
    unsigned char* inputBuffer,
    int length
)
{

}

//ErrorCode CreateOPUS(CAudioDecoder** ppCAudioDecode, AudioPayloadType /* adspPayloadType */, AESettingsRecv aeSettingsRecv)
//{
//    ErrorCode hr = S_OK;
//    AUDIO_DECODER_CLASS* pDecoder;
//
//    //assert(ppCAudioDecode);
//    //*ppCAudioDecode = nullptr;
//    //CAudioDecoderImpl* pDec = new CAudioDecoderImpl();
//    //if (!pDec) {
//    //    return AC_E_OUTOFMEMORY;
//    //}
//    //hr = audiocodecs::CAudioDecode_OPUS_c::CreateInstance(&pDecoder, AudioPayloadType::OPUS, aeSettingsRecv);
//    //if (FAILED(hr)) {
//    //    return hr;
//    //}
//    //pDec->SetDecoderHandle(pDecoder);
//    //*ppCAudioDecode = pDec;
//    return hr;
//}

//void* g_Codecs_CreateInstance[] = {
//    (void*)CreateOPUS,
//};

extern void* g_Codecs_CreateInstance[];
extern void* g_Codecs_DeleteInstance[];


void ValidateHealRatioMetrics(AudioPayloadType audioPayloadType, uint32_t samplingScaling,
    const uint8_t* payload, uint16_t bytesPerPacket, int numStartPushes, int numEndPushes, int pushsToSkip,
    int numOfPulls, int numDtxGaps, adspmetrics::AudioEngineQoEMetrics& audioEngineQoEMetrics,
    uint16_t opusDecSettings = 0,
    CoreAudioHealerModule::DelayControl JBdelayController = CoreAudioHealerModule::DelayControl::minMax)
{
    ErrorCode result = CAudioHealerErrorCodes::AH_S_OK;
    CAudioHealer* pAudioHealer;
    uint64_t initialSendTimestamp = 0xabcdef;  // random start
    uint32_t initialSequenceNumber = 1234;
    const uint16_t pTimeMs = 20;

    CAudioMetrics::RTPMetadata rtpInfo{};
    rtpInfo.adspPayloadType = audioPayloadType;
    rtpInfo.sendTimestamp = initialSendTimestamp;
    rtpInfo.recvTimestamp100ns = 0;
    rtpInfo.sequenceNumber = initialSequenceNumber;
    rtpInfo.mainSequenceNumber = 0;
    rtpInfo.SSRC = 0;
    rtpInfo.isValidDTMFTone = false;
    rtpInfo.markerBit = false;
    rtpInfo.isRedundantPacket = false;
    rtpInfo.redundancyTimestampOffset = 0;
    rtpInfo.NTPTimestamp = 0;
    rtpInfo.bytesPerPacket = bytesPerPacket;
    rtpInfo.isRtxRecoveredPacket = false;
    CAudioMetrics::HealerPushStats healerPushStats;
    int16_t tmpOutBuffer[320];  // 20ms at 16KHz
    int32_t outBuffSize = 640;
    CAudioMetrics::PCMMetadata pcmMeta;
    CAudioMetrics::HealerPullStats pullStats;
    std::vector<Decoder*> decoderDescriptors;
    Decoder* pDecoderDescriptor = nullptr;

    for (int i = 0; i < G_LNUMCODECS; i++) {
        if (audioPayloadType == G_CODECS[i]->adspPayloadType) {
            pDecoderDescriptor = new Decoder();
            //ASSERT_TRUE(pDecoderDescriptor != nullptr);
            pDecoderDescriptor->CreateDecodeInstance = g_Codecs_CreateInstance[i];
            //pDecoderDescriptor->DeleteDecodeInstance = g_Codecs_DeleteInstance[i];
            pDecoderDescriptor->lAlgDelay = G_CODECS[i]->lAlgDelay;
            pDecoderDescriptor->lBasicFrameSize = G_CODECS[i]->lBasicFrameSize;
            pDecoderDescriptor->lSamplingRate = G_CODECS[i]->lSamplingRate;
            pDecoderDescriptor->adspPayloadType = G_CODECS[i]->adspPayloadType;
            pDecoderDescriptor->psName = G_CODECS[i]->psName;

            decoderDescriptors.push_back(pDecoderDescriptor);
        }
    }

    CAudioMetrics::ConfigurationSettingsData configData;
    CAudioHealer::GetDefaultConfigurationSettings(configData);
    configData.EnableSpecializedAudioHealer = true;
    configData.OpusDecSettings = opusDecSettings;
    configData.ComputeFirstBackFill = true;
    configData.JBControllerVersion = ((uint32_t)JBdelayController) << 16;
    configData.JBDelayControlFallback &= ~((uint32_t)CoreAudioHealerModule::ControllerFallback::opusCallMask);

    result = CAudioHealer::CreateInstance(&decoderDescriptors, &pAudioHealer, audioPayloadType, &configData);
    //ASSERT_TRUE(result == CAudioHealerErrorCodes::AH_S_OK);


    for (int i = 0; i < numStartPushes; i++) {
        // Push a packet
        result = pAudioHealer->PushData(payload, bytesPerPacket, 0, rtpInfo, healerPushStats);
        //ASSERT_TRUE(result == CAudioHealerErrorCodes::AH_S_OK);
        result = pAudioHealer->PullPCM(20, 0, (uint8_t*)tmpOutBuffer, outBuffSize, pcmMeta, pullStats);
        //ASSERT_TRUE(result == CAudioHealerErrorCodes::AH_S_OK);
        rtpInfo.sendTimestamp += pTimeMs * samplingScaling;
        rtpInfo.sequenceNumber++;
    }

    for (int i = 0; i < numDtxGaps; i++) {  // simulate time-stamp gaps due to DTX mode
        result = pAudioHealer->PullPCM(20, 0, (uint8_t*)tmpOutBuffer, outBuffSize, pcmMeta, pullStats);
        //ASSERT_TRUE(result == CAudioHealerErrorCodes::AH_S_OK);
        rtpInfo.sendTimestamp += pTimeMs * samplingScaling;
    }

    for (int i = 0; i < numEndPushes; i++) {
        // Push a packet
        result = pAudioHealer->PushData(payload, bytesPerPacket, 0, rtpInfo, healerPushStats);
        //ASSERT_TRUE(result == CAudioHealerErrorCodes::AH_S_OK);
        result = pAudioHealer->PullPCM(20, 0, (uint8_t*)tmpOutBuffer, outBuffSize, pcmMeta, pullStats);
        //ASSERT_TRUE(result == CAudioHealerErrorCodes::AH_S_OK);
        rtpInfo.sendTimestamp += pTimeMs * samplingScaling;
        rtpInfo.sequenceNumber++;
    }

    if (pushsToSkip > 0) {
        rtpInfo.sendTimestamp += pTimeMs * samplingScaling * pushsToSkip;
        rtpInfo.sequenceNumber += pushsToSkip;

        result = pAudioHealer->PushData(payload, bytesPerPacket, 0, rtpInfo, healerPushStats);
        //ASSERT_TRUE(result == CAudioHealerErrorCodes::AH_S_OK);

        result = pAudioHealer->PullPCM(20, 0, (uint8_t*)tmpOutBuffer, outBuffSize, pcmMeta, pullStats);
        //ASSERT_TRUE(result == CAudioHealerErrorCodes::AH_S_OK);
        pAudioHealer->PullInfo(audioEngineQoEMetrics);
        //ASSERT_EQ(0, audioEngineQoEMetrics.healerMetrics.backFillCount);
    }

    for (int i = 1; i < numOfPulls; i++) {
        result = pAudioHealer->PullPCM(20, 0, (uint8_t*)tmpOutBuffer, outBuffSize, pcmMeta, pullStats);
        //ASSERT_TRUE(result == CAudioHealerErrorCodes::AH_S_OK);
    }

    pAudioHealer->PullInfo(audioEngineQoEMetrics);
    //ASSERT_EQ(0, audioEngineQoEMetrics.healerMetrics.backFillCount);

    CAudioHealer::DeleteInstance(pAudioHealer);
    pAudioHealer = nullptr;

    for (int i = 0; i < (int)decoderDescriptors.size(); i++) {
        //ASSERT_TRUE(decoderDescriptors[i] != nullptr);
        delete decoderDescriptors[i];
        decoderDescriptors[i] = nullptr;
    }
    decoderDescriptors.clear();

}

void TestJB()
{
    CAudioMetrics::ConfigurationSettingsData configData{};
    CAudioHealer::GetDefaultConfigurationSettings(configData);
    //configData.OpusDecSettings = opusDecSettings;  // Decode Opus in-band FEC and DTX packets, detect joined muted


    adspmetrics::AudioEngineQoEMetrics audioEngineQoEMetrics;
    const uint8_t OPUS_PAYLOAD[] = { 72, 11, 228, 226, 150, 185, 215, 174, 29, 48, 231, 109,
                                     208, 65, 194, 221, 128 };
    const uint32_t OPUS_MS_TO_SAMPLES_SCALING = 48;
    int32_t delayMethod[2] = { 0, 2 };
    for (int i = 0; i < 2; i++) {
        ValidateHealRatioMetrics(AudioPayloadType::OPUS, OPUS_MS_TO_SAMPLES_SCALING, OPUS_PAYLOAD, sizeof(OPUS_PAYLOAD),
            500, 500, 10, 9, 0, audioEngineQoEMetrics, 0,
            (CoreAudioHealerModule::DelayControl)delayMethod[i]);

        // check metrics
        //ASSERT_EQ(0, audioEngineQoEMetrics.healerMetrics.healedFramesAfterLastPacket);
        //ASSERT_LT(audioEngineQoEMetrics.healerMetrics.healedDataRatioLongTerm, 0.008);
        //ASSERT_LT(audioEngineQoEMetrics.healerMetrics.healedDataRatioLongTermDtx, 0.008);
    }
}

void Decode(
    unsigned char* inputBuffer,
    int length)
{
    using namespace audiocodecs;

    TestJB();

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

