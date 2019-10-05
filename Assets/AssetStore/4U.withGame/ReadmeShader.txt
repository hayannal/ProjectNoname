「シェーダーについて」About shaders



軽量なシェーダーを目指しつつ、様々なカラーバリエーションが作れるシェーダーです。

　It is a lightweight shader that makes color variations.



計算過程でfloatは使っていません。

　I do not use float in the calculation process.


頂点シェーダーでPOS,UV,HalfLambert,GlobalIlluminationを計算しています。

　Vertex shader calculates POS, UV, HalfLambert, GlobalIllumination.


1Passで描画できる事を念頭に置いてますので、Shadowing（投影、受影）は計算しません。

　Calculate only Shading, do not calculate Shadowing, draw with 1 pass.


その代わり、足元に影だけを表示させるオブジェクトを設置しています。

　Instead, we set up an object that displays only the shadow at the foot.
　(Use 4U.withGame/Lit/OnlyShadow)


シェーディングは行います。光源方向から影になる部分を計算します。

　Calculate the shadow from light source direction.



VRシングルパスステレオレンダリングに対応

　Supports VR single pass stereo rendering.





「設定項目について」About setting items


・Alpha CutOff
テクスチャのアルファ値が設定以下であれば描画しません。

 If the alpha value of the texture is less than or equal to the setting, it will not be drawn.


・Base（RGB)

ベーステクスチャです。Base texture


・Main Color

ベーステクスチャに乗算する色です。The color to be multiplied by the base texture.


・White Balance

ベーステクスチャにをRGBに加算します。When monochromatized, add the value to RGB.


・Hue Change

色相を変化します。
It changes hue.


・Shade Color
影の色です。
Shade Color.


・Shade Shift (UnlitShade only)

影の強度です。-1で全て影色、1で影なしです。
It is the strength of the shade. -1 means all shade color, 1 means no shade.


・No Shading Map

シェーディングを行わない部分です。エミッション的に使えます。

It is a part which does not do shading. It can be used like emissions.


・GI Intensity
GI
を加算する強度です。
It is intensity to add global illumination.





「計算手順」Calculation procedure

Base(RGB)テクスチャ読み込み

　Texture reading

CutOFFでアルファ部分をクリップ

　Clip the alpha part with CutOFF

ホワイトバランスを加算して、Colorを乗算。

　Add white balance and multiply by Color.

色相を変化。

　Changes hue.

法面から影になる部分を影色で乗算

　Multiply shade parts from shadow side

 （No Shading Map が0.5以上に明るい部分は元の色を描画）

　(No Shading Map draws the original color where the bright part is over 0.5)

グローバルイルミネーションを加算

　Add Global Illumination

フォグを計算

　Calculate fog

最終描画

　Final drawing





-About FPS Test shader-

Render "Queue" = "Transparent" に設定し、50体表示用のPassCallsを増やすためのシェーダーです。

Render "Queue" = "Transparent" and shader to earn PassCalls for 50 body display.





感想や、質問等は随時お待ちしております。

I am waiting for feedback and questions etc from time to time.

ここまで読んでくださり、ありがとうございました！

Thank you very much for reading so far!






version1.1  2019/1/24

VRシングルパスステレオレンダリングに対応

Supports VR single pass stereo rendering.



version1.0  2018/12/13


First release 