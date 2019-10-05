「シェーダーについて」About shaders



軽量なシェーダーを目指しつつ、様々なカラーバリエーションが作れるトゥーンシェーダーです。

　It is a lightweight Toon shader that makes color variations.

VRシングルパスステレオレンダリングに対応

　Supports VR single pass stereo rendering.





シェーダー対応状況　Shader compatibility

・SurfToon　
　2017.4以降の全バージョンで動作確認。　Operation confirmation with all versions after 2017.4.
　GPUインスタンシング対応。マテリアルプロパティブロック　"_HueCI"　で色相変化が可能。　GPU instancing support. Hue change with material property block "_HueCI".

・LWRPToon
　Unity	2018.3 GPUインスタンシング対応(マテリアルプロパティブロックは使えません)　Unity 2018.3 GPU instantiation supported (Material property block can not be used)
　Unity 2019.1 SRPバッチング対応。スクリプトで色相変化のために "_HueCB" を追加。　Unity 2019.1 SRP batching support. Added "_HueCB" for hue change in script.

・LWRPToon18.2
  Unity 2018.2 のみに対応。(マテリアルプロパティブロックは使えません)　It corresponds only to Unity 2018.2. (Material property block can not be used)





「設定項目について」About setting items


・Alpha CutOff
テクスチャのアルファ値が設定以下であれば描画しません。

 If the alpha value of the texture is less than or equal to the setting, it will not be drawn.


・Base（RGB)

ベーステクスチャです。Base texture


・Main Color

ベーステクスチャに乗算する色です。The color to be multiplied by the base texture.


・Keep White

白目等、MainColorを載せたくない場合に使用します。Used when you do not want to place MainColor such as white eyes etc.


・Hue Change

色相を変化します。
It changes hue.


・Shade Color
影の色です。
Shade Color.


・Shade Shift (UnlitShade only)

影の強度です。-1で全て影色、1で影なしです。
It is the strength of the shade. -1 means all shade color, 1 means no shade.


・Emission Color

エミッション部分に色を乗算します。　Multiply the emission part by color.



・Emission Map

エミッションマップです。It is an emission map.


・GI Intensity
GIを加算する強度です。
It is intensity to add global illumination.





「計算手順」Calculation procedure

Base(RGB)テクスチャ読み込み

　Texture reading

CutOFFでアルファ部分をクリップ

　Clip the alpha part with CutOFF

Colorを乗算。(KeepWhiteは白い部分を線形補間し除く)

　Multiply by Color.(KeepWhite excludes linear interpolation of white parts)

色相を変化。

　Changes hue.

法面から影になる部分を影色で乗算

　Multiply shade parts from shadow side

グローバルイルミネーションを加算

　Add Global Illumination

エミッション部分の表示

　Display of emission part

フォグを計算

　Calculate fog

最終描画

　Final drawing








感想や、質問等は随時お待ちしております。

I am waiting for feedback and questions etc from time to time.

ここまで読んでくださり、ありがとうございました！

Thank you very much for reading so far!




version2.0  2019/2/27
サーフェイスシェーダーへの変更
LWRPシェーダーの追加
Change to a surface shader.
Add LWRP shader.


version1.1  2019/1/24
VRシングルパスステレオレンダリングに対応
Supports VR single pass stereo rendering.


version1.0  2018/12/13
First release 