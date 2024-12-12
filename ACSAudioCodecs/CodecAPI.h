#pragma once

extern "C" {
    __declspec(dllexport) void Encode(unsigned char* inputBuffer, int length);
    __declspec(dllexport) void Decode(unsigned char* inputBuffer, int length);
}