#ifndef SOBELOUTLINES_INCLUDED
#define SOBELOUTLINES_INCLUDED

static float2 sobelSamplePoints[9] =
{
    float2(-1, 1), float2(0, 1), float2(1, 1),
    float2(-1, 0), float2(0, 0), float2(1, 0),
    float2(-1, -1), float2(0, -1), float2(1, -1),
};

// Weights for the x component
static float sobelXMatrix[9] =
{
    1, 0, -1,
    2, 0, -2,
    1, 0, -1
};

// Weights for the y component
static float sobelYMatrix[9] =
{
    1, 2, 1,
    0, 0, 0,
    -1, -2, -1
};


// This function runs the sobel algorithm over the depth texture
void DepthSobel_float(float2 UV, float2 Thickness, out float Out, out float AverageDepth)
{
    float2 sobel = 0;
    // We can unroll this loop to make it more efficient
    // The compiler is also smart enough to remove the i=4 iteration, which is always zero
    float averageDepth = 0;
    [unroll]
    for (int i = 0; i < 9; i++)
    {
        //float depth = Linear01Depth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(
        //UV + sobelSamplePoints[i] * Thickness), _ZBufferParams);
       
        
        // Adjust thickness separately for X and Y
        float depth = Linear01Depth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(
        UV + sobelSamplePoints[i] * Thickness), _ZBufferParams);
       
        float proximityWeight = saturate(1.0 - depth); // More influence for close objects
        float rawDepth = SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV + sobelSamplePoints[i] * Thickness);
        
        sobel += rawDepth * float2(sobelXMatrix[i], sobelYMatrix[i]) * proximityWeight;
        
        //sobel += depth * float2(sobelXMatrix[i], sobelYMatrix[i]);
        averageDepth += rawDepth;
    }

    // Get the final Sobel value
    float sobelValue = length(sobel);
    
    Out = sobelValue;
    AverageDepth = averageDepth / 9;
}

// This function runs the sobel algorithm over the opaque texture
void ColorSobel_float(float2 UV, float Thickness, out float Out)
{
    // We have to run the sobel algorithm over the RGB channels separately
    float2 sobelR = 0;
    float2 sobelG = 0;
    float2 sobelB = 0;
    // We can unroll this loop to make it more efficient
    // The compiler is also smart enough to remove the i=4 iteration, which is always zero
    [unroll]
    for (int i = 0; i < 9; i++)
    {
        // Sample the scene color texture
        float3 rgb = SHADERGRAPH_SAMPLE_SCENE_COLOR(UV + sobelSamplePoints[i] * Thickness);
        // Create the kernel for this iteration
        float2 kernel = float2(sobelXMatrix[i], sobelYMatrix[i]);
        // Accumulate samples for each color
        sobelR += rgb.r * kernel;
        sobelG += rgb.g * kernel;
        sobelB += rgb.b * kernel;
    }
    // Get the final sobel value
    // Combine the RGB values by taking the one with the largest sobel value
    Out = max(length(sobelR), max(length(sobelG), length(sobelB)));
    // This is an alternate way to combine the three sobel values by taking the average
    // See which one you like better
    //Out = (length(sobelR) + length(sobelG) + length(sobelB)) / 3.0;
}
#endif