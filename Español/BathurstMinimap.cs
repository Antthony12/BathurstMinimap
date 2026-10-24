using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GTA;
using GTA.UI;

/// <summary>
/// v1.0.0 - Muestra un pequeño mapa superpuesto de Bathurst con un marcador
/// en vivo (pin.png) que rastrea la posición del jugador. El mapa solo aparece
/// mientras el jugador esté cerca del circuito real, usando puntos de telemetría
/// reales (incrustados más abajo) como línea de referencia. La posición/tamaño
/// del mapa y el radio de deteccion se configuran mediante BathurstMinimap.ini.
/// </summary>
public class BathurstMinimap : Script
{
    private CustomSprite mapSprite;
    private CustomSprite pinSprite;
    private bool enabled = true;

    // Posición y tamaño en pantalla de la superposición del mapa, cargados desde BathurstMinimap.ini
    // (usa estos valores por defecto si el archivo o un valor falta/es inválido).
    private PointF mapScreenPos = new PointF(-80f, 0f);
    private SizeF mapScreenSize = new SizeF(400f, 400f);
    private SizeF pinSize = new SizeF(6f, 6f);

    // Si el mod del circuito se mueve de sitio en el mundo de GTA (traslacion pura,
    // sin rotar), pon aqui cuanto se ha desplazado y el mapa lo compensa solo.
    // No sirve si ademas rotaron el circuito: en ese caso hay que regrabar la vuelta.
    private float worldOffsetX = 0f;
    private float worldOffsetY = 0f;

    // Qué tan cerca (en unidades del juego, aproximadamente metros) necesita estar el jugador
    // de la línea grabada para considerarse "en el circuito". Configurable via .ini.
    private float ON_TRACK_DISTANCE = 20f;

    private const float CANVAS = 972f;

    // --- Transformación mundo -> píxel del lienzo Bathurst.png (972x972) ---
    // Derivada del bounding box de una vuelta grabada real.
    private const float MIN_X = 6039.28f;
    private const float MIN_Y = 13907.72f;
    private const float SCALE = 0.4165265793f;
    private const float OFF_X = 242.7214035f;
    private const float OFF_Y = 40.0000000f;

    // Telemetría real de una vuelta grabada (~674 puntos, uno cada ~9m), usada
    // solo para detectar si el jugador está cerca del circuito. Incrustada
    // directamente aquí para no necesitar ningún archivo de datos aparte.
    private static readonly float[] TRACK_X = {
            6998.65f, 6989.26f, 6980.15f, 6970.92f, 6961.49f, 6952.13f, 6943.07f, 6933.74f, 6924.30f, 6915.12f,
            6905.78f, 6896.32f, 6887.26f, 6878.07f, 6868.76f, 6859.30f, 6850.26f, 6841.13f, 6831.92f, 6822.61f,
            6813.21f, 6803.74f, 6794.16f, 6785.11f, 6775.98f, 6766.85f, 6757.70f, 6748.59f, 6739.11f, 6729.87f,
            6720.35f, 6711.09f, 6701.69f, 6692.57f, 6683.46f, 6674.37f, 6665.17f, 6655.93f, 6646.69f, 6637.85f,
            6629.83f, 6623.13f, 6618.21f, 6615.57f, 6614.52f, 6614.56f, 6614.54f, 6614.02f, 6613.85f, 6614.17f,
            6614.44f, 6614.18f, 6613.84f, 6613.91f, 6614.20f, 6614.08f, 6613.69f, 6613.42f, 6613.23f, 6613.04f,
            6612.89f, 6612.78f, 6612.69f, 6612.61f, 6612.53f, 6612.47f, 6612.42f, 6612.26f, 6611.88f, 6611.38f,
            6610.95f, 6610.62f, 6610.33f, 6610.06f, 6609.78f, 6609.49f, 6609.26f, 6609.14f, 6609.08f, 6609.03f,
            6608.98f, 6608.99f, 6609.04f, 6609.10f, 6609.17f, 6609.24f, 6609.32f, 6609.41f, 6609.45f, 6609.35f,
            6609.13f, 6608.87f, 6608.64f, 6608.46f, 6608.31f, 6608.20f, 6608.15f, 6608.12f, 6608.10f, 6608.08f,
            6608.07f, 6608.06f, 6608.06f, 6608.06f, 6607.95f, 6607.72f, 6607.50f, 6607.38f, 6607.29f, 6607.18f,
            6607.02f, 6606.84f, 6606.60f, 6606.33f, 6606.06f, 6605.84f, 6605.69f, 6605.60f, 6605.52f, 6605.43f,
            6605.33f, 6605.22f, 6605.10f, 6604.97f, 6604.82f, 6604.68f, 6604.54f, 6604.42f, 6604.30f, 6604.20f,
            6604.11f, 6604.03f, 6603.95f, 6603.87f, 6603.80f, 6603.69f, 6603.39f, 6602.89f, 6602.35f, 6601.91f,
            6601.58f, 6601.36f, 6601.26f, 6601.25f, 6601.26f, 6601.29f, 6601.34f, 6601.39f, 6601.44f, 6601.48f,
            6601.52f, 6601.56f, 6601.61f, 6601.66f, 6601.69f, 6601.66f, 6601.57f, 6601.19f, 6599.85f, 6597.18f,
            6593.78f, 6589.34f, 6583.72f, 6577.57f, 6570.78f, 6563.03f, 6554.35f, 6545.60f, 6536.68f, 6527.75f,
            6518.67f, 6509.64f, 6500.44f, 6491.42f, 6482.32f, 6473.58f, 6464.74f, 6456.15f, 6447.46f, 6438.69f,
            6430.26f, 6421.73f, 6412.97f, 6404.52f, 6395.92f, 6387.14f, 6378.28f, 6369.39f, 6360.45f, 6351.53f,
            6342.62f, 6334.12f, 6325.56f, 6317.00f, 6308.39f, 6299.87f, 6291.12f, 6282.65f, 6274.08f, 6265.35f,
            6256.44f, 6247.76f, 6238.94f, 6229.90f, 6220.77f, 6211.64f, 6202.56f, 6193.67f, 6184.93f, 6176.18f,
            6167.98f, 6160.04f, 6152.03f, 6144.17f, 6137.55f, 6133.07f, 6130.85f, 6131.31f, 6134.34f, 6138.85f,
            6145.25f, 6152.17f, 6158.99f, 6165.81f, 6172.86f, 6179.51f, 6186.34f, 6193.05f, 6199.64f, 6205.84f,
            6211.62f, 6216.46f, 6220.21f, 6223.07f, 6225.50f, 6227.07f, 6227.49f, 6227.65f, 6227.94f, 6228.25f,
            6228.45f, 6228.52f, 6228.53f, 6228.48f, 6228.35f, 6228.02f, 6227.57f, 6226.87f, 6225.45f, 6223.32f,
            6220.61f, 6217.08f, 6212.40f, 6206.54f, 6199.85f, 6192.57f, 6184.85f, 6176.57f, 6168.45f, 6160.74f,
            6153.42f, 6146.60f, 6140.32f, 6134.22f, 6128.42f, 6122.69f, 6117.01f, 6111.64f, 6106.28f, 6101.03f,
            6095.67f, 6090.36f, 6085.35f, 6080.29f, 6075.16f, 6070.12f, 6065.15f, 6060.78f, 6057.34f, 6054.57f,
            6052.56f, 6050.91f, 6048.93f, 6046.97f, 6045.19f, 6043.56f, 6042.00f, 6040.62f, 6039.85f, 6039.43f,
            6039.28f, 6040.02f, 6041.62f, 6043.87f, 6046.61f, 6049.73f, 6053.86f, 6058.91f, 6064.29f, 6070.14f,
            6076.26f, 6082.55f, 6088.40f, 6094.24f, 6100.08f, 6106.04f, 6112.18f, 6118.45f, 6124.77f, 6131.13f,
            6137.15f, 6143.19f, 6149.28f, 6155.38f, 6161.57f, 6167.56f, 6173.67f, 6179.96f, 6186.38f, 6193.02f,
            6200.40f, 6208.06f, 6216.56f, 6225.13f, 6233.52f, 6242.18f, 6250.91f, 6260.14f, 6269.37f, 6278.46f,
            6287.91f, 6297.27f, 6306.25f, 6315.48f, 6324.95f, 6334.08f, 6343.37f, 6352.76f, 6361.82f, 6371.20f,
            6380.65f, 6390.15f, 6399.11f, 6408.11f, 6417.16f, 6426.24f, 6435.23f, 6444.62f, 6453.87f, 6463.19f,
            6472.32f, 6481.25f, 6490.41f, 6499.54f, 6508.81f, 6517.87f, 6527.07f, 6536.00f, 6544.88f, 6553.86f,
            6562.52f, 6571.10f, 6579.71f, 6588.11f, 6597.04f, 6606.12f, 6615.13f, 6624.03f, 6632.80f, 6641.68f,
            6650.53f, 6659.39f, 6668.62f, 6677.54f, 6685.87f, 6693.57f, 6700.70f, 6706.79f, 6711.50f, 6715.88f,
            6722.51f, 6730.98f, 6740.03f, 6749.07f, 6758.19f, 6767.41f, 6776.57f, 6785.28f, 6793.51f, 6801.49f,
            6809.17f, 6816.70f, 6824.27f, 6832.13f, 6839.85f, 6847.61f, 6855.53f, 6863.21f, 6871.17f, 6879.63f,
            6888.48f, 6897.52f, 6906.58f, 6915.56f, 6924.57f, 6933.34f, 6941.67f, 6949.70f, 6957.30f, 6964.21f,
            6970.37f, 6976.17f, 6981.84f, 6987.42f, 6992.84f, 6997.76f, 7002.74f, 7008.57f, 7014.78f, 7022.11f,
            7030.47f, 7039.44f, 7048.31f, 7056.33f, 7062.91f, 7067.68f, 7071.58f, 7075.62f, 7079.82f, 7083.93f,
            7087.96f, 7091.55f, 7094.85f, 7098.31f, 7101.87f, 7105.39f, 7108.66f, 7112.00f, 7115.40f, 7118.83f,
            7122.12f, 7125.43f, 7128.57f, 7131.40f, 7134.08f, 7136.67f, 7138.66f, 7139.98f, 7140.74f, 7141.25f,
            7141.63f, 7141.84f, 7141.88f, 7141.83f, 7141.69f, 7141.52f, 7141.43f, 7141.46f, 7141.55f, 7141.64f,
            7141.73f, 7141.83f, 7141.94f, 7142.05f, 7142.15f, 7142.24f, 7142.33f, 7142.36f, 7142.31f, 7142.22f,
            7142.12f, 7142.02f, 7141.91f, 7141.81f, 7141.71f, 7141.60f, 7141.50f, 7141.40f, 7141.30f, 7141.20f,
            7141.10f, 7140.97f, 7140.78f, 7140.58f, 7140.39f, 7140.19f, 7140.03f, 7139.94f, 7139.88f, 7139.83f,
            7139.79f, 7139.76f, 7139.73f, 7139.70f, 7139.67f, 7139.63f, 7139.58f, 7139.53f, 7139.48f, 7139.43f,
            7139.37f, 7139.30f, 7139.21f, 7139.09f, 7138.95f, 7138.77f, 7138.56f, 7138.33f, 7138.09f, 7137.87f,
            7137.67f, 7137.54f, 7137.44f, 7137.36f, 7137.28f, 7137.22f, 7137.17f, 7137.11f, 7137.06f, 7136.99f,
            7136.87f, 7136.67f, 7136.41f, 7136.16f, 7135.96f, 7135.84f, 7135.86f, 7136.00f, 7136.18f, 7136.36f,
            7136.50f, 7136.45f, 7136.19f, 7135.92f, 7135.91f, 7136.15f, 7136.48f, 7136.70f, 7136.76f, 7136.71f,
            7136.57f, 7136.37f, 7136.14f, 7135.98f, 7135.94f, 7135.95f, 7135.90f, 7135.82f, 7135.74f, 7135.67f,
            7135.60f, 7135.53f, 7135.47f, 7135.41f, 7135.36f, 7135.29f, 7135.13f, 7134.93f, 7134.79f, 7134.74f,
            7134.74f, 7134.64f, 7134.44f, 7134.37f, 7134.50f, 7134.74f, 7135.26f, 7136.27f, 7137.79f, 7139.70f,
            7141.98f, 7144.47f, 7147.07f, 7149.72f, 7152.44f, 7155.22f, 7158.02f, 7160.77f, 7163.46f, 7166.10f,
            7168.73f, 7171.30f, 7173.79f, 7176.26f, 7178.71f, 7181.15f, 7183.59f, 7186.05f, 7188.69f, 7191.18f,
            7193.77f, 7196.42f, 7199.19f, 7201.82f, 7204.46f, 7206.87f, 7207.74f, 7206.07f, 7201.48f, 7194.92f,
            7187.52f, 7180.23f, 7172.93f, 7165.63f, 7158.73f, 7152.52f, 7146.85f, 7141.92f, 7137.82f, 7134.30f,
            7131.76f, 7130.41f, 7129.98f, 7130.02f, 7129.97f, 7129.62f, 7129.40f, 7129.55f, 7129.81f, 7129.93f,
            7129.88f, 7129.72f, 7129.46f, 7129.12f, 7128.77f, 7128.51f, 7128.52f, 7128.72f, 7128.92f, 7128.97f,
            7128.87f, 7128.72f, 7128.61f, 7128.50f, 7128.38f, 7128.27f, 7128.15f, 7128.06f, 7127.97f, 7127.88f,
            7127.83f, 7127.89f, 7128.04f, 7128.29f, 7128.53f, 7128.66f, 7128.69f, 7128.68f, 7128.60f, 7128.43f,
            7128.23f, 7128.01f, 7127.79f, 7127.57f, 7127.36f, 7127.22f, 7127.12f, 7126.75f, 7124.30f, 7118.46f,
            7110.16f, 7101.27f, 7091.94f, 7082.76f, 7073.54f, 7064.24f, 7054.97f, 7045.82f, 7036.81f, 7027.49f,
            7018.02f, 7008.85f, 6999.36f, 6998.65f,
    };

    private static readonly float[] TRACK_Y = {
            16045.42f, 16045.34f, 16045.28f, 16045.21f, 16045.19f, 16045.25f, 16045.35f, 16045.47f, 16045.60f, 16045.73f,
            16045.85f, 16045.97f, 16046.09f, 16046.20f, 16046.33f, 16046.46f, 16046.59f, 16046.73f, 16046.89f, 16047.03f,
            16047.17f, 16047.21f, 16047.09f, 16046.91f, 16046.86f, 16047.00f, 16047.22f, 16047.46f, 16047.69f, 16047.90f,
            16048.12f, 16048.28f, 16048.33f, 16048.31f, 16048.27f, 16048.23f, 16048.18f, 16048.12f, 16047.83f, 16046.21f,
            16042.05f, 16035.91f, 16028.13f, 16019.37f, 16010.22f, 16000.96f, 15991.77f, 15982.65f, 15973.53f, 15964.48f,
            15955.45f, 15946.18f, 15936.72f, 15927.46f, 15918.27f, 15908.78f, 15899.71f, 15890.57f, 15881.36f, 15872.03f,
            15862.45f, 15853.39f, 15844.30f, 15835.18f, 15826.01f, 15816.79f, 15807.55f, 15798.32f, 15789.13f, 15779.91f,
            15770.64f, 15761.31f, 15751.96f, 15742.54f, 15733.08f, 15723.56f, 15714.00f, 15704.42f, 15694.82f, 15685.81f,
            15676.74f, 15667.54f, 15658.31f, 15649.03f, 15639.77f, 15630.53f, 15621.32f, 15612.07f, 15602.80f, 15593.54f,
            15584.19f, 15574.75f, 15565.35f, 15555.90f, 15546.40f, 15536.84f, 15527.28f, 15517.68f, 15508.04f, 15498.44f,
            15488.82f, 15479.19f, 15469.57f, 15459.95f, 15450.30f, 15440.63f, 15430.96f, 15421.36f, 15411.81f, 15402.21f,
            15392.53f, 15383.48f, 15374.37f, 15365.23f, 15356.08f, 15346.89f, 15337.69f, 15328.53f, 15319.35f, 15310.14f,
            15300.87f, 15291.55f, 15282.16f, 15272.73f, 15263.27f, 15253.75f, 15244.24f, 15234.65f, 15225.15f, 15215.65f,
            15206.16f, 15196.63f, 15187.16f, 15177.65f, 15168.12f, 15158.59f, 15149.08f, 15139.60f, 15130.12f, 15120.57f,
            15111.03f, 15101.51f, 15092.02f, 15082.51f, 15072.97f, 15063.54f, 15054.17f, 15044.83f, 15035.48f, 15026.31f,
            15016.72f, 15007.15f, 14998.12f, 14988.62f, 14979.50f, 14970.22f, 14960.95f, 14951.63f, 14942.66f, 14933.90f,
            14925.55f, 14917.58f, 14910.45f, 14903.71f, 14897.61f, 14892.80f, 14889.47f, 14886.39f, 14883.92f, 14882.75f,
            14882.69f, 14883.71f, 14885.57f, 14887.75f, 14890.22f, 14892.97f, 14896.05f, 14899.18f, 14902.50f, 14905.93f,
            14909.25f, 14912.61f, 14916.07f, 14919.47f, 14922.83f, 14925.96f, 14928.95f, 14932.06f, 14935.37f, 14938.74f,
            14942.08f, 14945.20f, 14948.31f, 14951.44f, 14954.58f, 14957.70f, 14960.91f, 14963.99f, 14966.96f, 14969.79f,
            14972.60f, 14975.15f, 14977.05f, 14978.29f, 14978.43f, 14977.45f, 14975.74f, 14972.82f, 14969.88f, 14966.91f,
            14963.11f, 14958.81f, 14954.38f, 14949.59f, 14943.41f, 14935.37f, 14926.57f, 14917.63f, 14908.96f, 14901.18f,
            14894.71f, 14888.72f, 14882.76f, 14876.87f, 14870.68f, 14864.55f, 14858.17f, 14851.89f, 14845.57f, 14838.95f,
            14831.63f, 14823.60f, 14815.07f, 14806.52f, 14797.57f, 14788.58f, 14779.58f, 14770.31f, 14761.26f, 14751.99f,
            14742.57f, 14733.50f, 14724.39f, 14715.15f, 14705.85f, 14696.44f, 14687.45f, 14678.45f, 14669.56f, 14660.40f,
            14651.51f, 14642.94f, 14635.19f, 14628.03f, 14621.48f, 14615.76f, 14610.99f, 14606.56f, 14601.99f, 14596.96f,
            14591.32f, 14585.30f, 14578.83f, 14571.91f, 14564.94f, 14557.68f, 14550.12f, 14542.70f, 14535.10f, 14527.78f,
            14520.31f, 14512.63f, 14505.15f, 14497.52f, 14489.84f, 14482.34f, 14474.48f, 14466.08f, 14457.65f, 14448.85f,
            14439.72f, 14430.68f, 14421.43f, 14412.33f, 14403.39f, 14394.28f, 14385.02f, 14375.93f, 14366.92f, 14357.50f,
            14348.19f, 14338.79f, 14329.57f, 14320.62f, 14311.61f, 14303.06f, 14294.76f, 14286.78f, 14279.48f, 14272.31f,
            14265.21f, 14258.01f, 14251.08f, 14243.93f, 14236.75f, 14229.49f, 14222.29f, 14215.14f, 14207.99f, 14200.80f,
            14193.99f, 14187.12f, 14180.18f, 14173.26f, 14166.23f, 14159.41f, 14152.46f, 14145.47f, 14138.81f, 14132.68f,
            14126.79f, 14121.70f, 14117.36f, 14113.75f, 14110.20f, 14106.72f, 14104.03f, 14101.96f, 14100.32f, 14099.24f,
            14099.37f, 14100.95f, 14102.89f, 14104.48f, 14105.74f, 14106.83f, 14107.92f, 14109.06f, 14110.27f, 14111.61f,
            14112.96f, 14114.32f, 14115.60f, 14116.90f, 14118.21f, 14119.53f, 14120.85f, 14122.22f, 14123.56f, 14124.91f,
            14126.22f, 14127.51f, 14128.83f, 14130.15f, 14131.48f, 14132.73f, 14133.00f, 14131.71f, 14130.00f, 14127.82f,
            14124.73f, 14121.57f, 14118.17f, 14114.68f, 14112.43f, 14111.72f, 14112.01f, 14113.64f, 14116.44f, 14119.44f,
            14121.60f, 14123.43f, 14123.82f, 14121.36f, 14117.56f, 14112.77f, 14106.91f, 14100.30f, 14092.47f, 14084.51f,
            14078.50f, 14075.29f, 14075.33f, 14076.74f, 14077.59f, 14077.99f, 14077.52f, 14074.92f, 14070.68f, 14065.72f,
            14060.41f, 14055.29f, 14050.37f, 14045.37f, 14040.48f, 14035.57f, 14030.56f, 14025.70f, 14020.93f, 14016.79f,
            14013.71f, 14011.43f, 14009.83f, 14008.21f, 14005.85f, 14002.57f, 13999.06f, 13994.74f, 13989.25f, 13982.93f,
            13975.96f, 13968.80f, 13961.75f, 13954.57f, 13946.96f, 13939.32f, 13931.79f, 13924.57f, 13918.01f, 13912.36f,
            13908.84f, 13907.72f, 13909.21f, 13913.38f, 13919.77f, 13927.42f, 13935.63f, 13943.75f, 13951.88f, 13959.96f,
            13968.50f, 13977.03f, 13985.52f, 13994.21f, 14002.64f, 14011.35f, 14019.74f, 14028.29f, 14036.95f, 14045.75f,
            14054.16f, 14062.68f, 14071.34f, 14080.23f, 14089.31f, 14098.56f, 14107.56f, 14116.93f, 14126.45f, 14136.09f,
            14145.14f, 14154.32f, 14163.61f, 14173.00f, 14182.48f, 14192.10f, 14201.67f, 14211.31f, 14220.32f, 14229.32f,
            14238.37f, 14247.39f, 14256.47f, 14265.68f, 14275.04f, 14284.47f, 14293.94f, 14303.39f, 14312.88f, 14322.34f,
            14331.81f, 14341.27f, 14350.69f, 14360.14f, 14369.54f, 14378.97f, 14388.34f, 14397.74f, 14407.12f, 14416.54f,
            14425.89f, 14435.28f, 14444.69f, 14454.13f, 14463.52f, 14472.90f, 14482.28f, 14491.63f, 14500.96f, 14510.27f,
            14519.58f, 14528.85f, 14538.12f, 14547.38f, 14556.60f, 14565.86f, 14575.13f, 14584.38f, 14593.63f, 14602.87f,
            14612.15f, 14621.35f, 14630.54f, 14639.73f, 14649.00f, 14658.19f, 14667.33f, 14676.40f, 14685.44f, 14694.46f,
            14703.51f, 14712.57f, 14721.66f, 14730.78f, 14739.91f, 14749.05f, 14758.21f, 14767.35f, 14776.46f, 14785.58f,
            14794.74f, 14803.87f, 14812.97f, 14822.10f, 14831.20f, 14840.37f, 14849.43f, 14858.47f, 14867.48f, 14876.49f,
            14886.24f, 14895.97f, 14905.70f, 14915.41f, 14925.14f, 14934.20f, 14943.30f, 14952.41f, 14961.54f, 14970.70f,
            14979.83f, 14988.95f, 14998.11f, 15007.22f, 15016.35f, 15025.53f, 15034.67f, 15043.71f, 15052.75f, 15061.76f,
            15070.77f, 15079.79f, 15088.82f, 15098.54f, 15108.24f, 15117.25f, 15126.97f, 15135.98f, 15144.98f, 15154.68f,
            15164.38f, 15174.10f, 15183.84f, 15193.53f, 15203.18f, 15212.19f, 15221.19f, 15230.23f, 15239.19f, 15248.07f,
            15256.91f, 15265.66f, 15274.38f, 15283.13f, 15291.89f, 15300.66f, 15309.46f, 15318.25f, 15327.05f, 15335.85f,
            15344.69f, 15353.52f, 15362.34f, 15371.19f, 15380.02f, 15388.81f, 15397.61f, 15406.41f, 15415.75f, 15424.42f,
            15433.13f, 15441.81f, 15450.87f, 15459.48f, 15468.14f, 15477.04f, 15486.02f, 15494.84f, 15502.81f, 15509.10f,
            15514.82f, 15520.30f, 15525.84f, 15531.72f, 15537.93f, 15544.57f, 15551.91f, 15559.93f, 15568.05f, 15576.64f,
            15585.74f, 15594.66f, 15603.76f, 15613.03f, 15622.46f, 15631.46f, 15640.57f, 15649.76f, 15659.10f, 15668.51f,
            15678.00f, 15687.01f, 15696.20f, 15705.55f, 15714.84f, 15724.19f, 15733.61f, 15743.14f, 15752.76f, 15761.79f,
            15770.89f, 15780.16f, 15789.48f, 15798.93f, 15808.44f, 15817.93f, 15827.52f, 15837.15f, 15846.80f, 15855.82f,
            15864.92f, 15873.99f, 15883.20f, 15892.48f, 15901.75f, 15911.05f, 15920.39f, 15929.76f, 15939.11f, 15948.49f,
            15957.72f, 15967.25f, 15976.38f, 15985.42f, 15994.79f, 16004.02f, 16013.30f, 16022.33f, 16031.13f, 16038.06f,
            16041.81f, 16043.26f, 16043.64f, 16043.47f, 16043.58f, 16043.79f, 16043.97f, 16044.15f, 16044.32f, 16044.53f,
            16044.79f, 16045.03f, 16045.32f, 16045.42f,
    };

    private readonly string scriptDir;
    private readonly Dictionary<string, Dictionary<string, string>> ini = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

    public BathurstMinimap()
    {
        // Los scripts compilados al vuelo por SHVDN a partir de archivos .cs sueltos no tienen
        // un archivo de ensamblado real en disco, por lo que Assembly.Location está vacío. En su lugar,
        // localiza la carpeta "scripts" del juego a partir de la ruta del ejecutable en ejecución.
        scriptDir = Path.Combine(Application.StartupPath, "scripts");

        LoadIni();
        ApplyIniValues();

        mapSprite = new CustomSprite(Path.Combine(scriptDir, "BathurstMinimap/Bathurst.png"), mapScreenSize, mapScreenPos);
        pinSprite = new CustomSprite(Path.Combine(scriptDir, "BathurstMinimap/pin.png"), pinSize, mapScreenPos);

        Tick += OnTick;
    }

    /// <summary>
    /// Lector INI muy pequeño: secciones entre [Corchetes], líneas key=value,
    /// ";" o "#" para comentarios. No requiere dependencias externas.
    /// </summary>
    private void LoadIni()
    {
        string path = Path.Combine(scriptDir, "BathurstMinimap.ini");
        if (!File.Exists(path))
        {
            Notification.PostTicker("~y~BathurstMinimap.ini no encontrado, se usaran valores por defecto.", false);
            return;
        }

        string currentSection = "";
        foreach (var raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#"))
                continue;

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                currentSection = line.Substring(1, line.Length - 2).Trim();
                if (!ini.ContainsKey(currentSection))
                    ini[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                continue;
            }

            int eq = line.IndexOf('=');
            if (eq < 0) continue;

            string key = line.Substring(0, eq).Trim();
            string value = line.Substring(eq + 1).Trim();

            if (!ini.ContainsKey(currentSection))
                ini[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            ini[currentSection][key] = value;
        }
    }

    private float GetIniFloat(string section, string key, float defaultValue)
    {
        Dictionary<string, string> kv;
        string raw;
        if (ini.TryGetValue(section, out kv) && kv.TryGetValue(key, out raw))
        {
            float parsed;
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                return parsed;

            Notification.PostTicker("~r~Valor invalido en BathurstMinimap.ini [" + section + "] " + key + "=" + raw + ", usando " + defaultValue.ToString(CultureInfo.InvariantCulture), false);
        }
        return defaultValue;
    }

    private void ApplyIniValues()
    {
        float posX = GetIniFloat("Map", "PosX", mapScreenPos.X);
        float posY = GetIniFloat("Map", "PosY", mapScreenPos.Y);
        float w = GetIniFloat("Map", "Width", mapScreenSize.Width);
        float h = GetIniFloat("Map", "Height", mapScreenSize.Height);
        float pinW = GetIniFloat("Pin", "Width", pinSize.Width);
        float pinH = GetIniFloat("Pin", "Height", pinSize.Height);
        worldOffsetX = GetIniFloat("Circuit", "OffsetX", worldOffsetX);
        worldOffsetY = GetIniFloat("Circuit", "OffsetY", worldOffsetY);
        ON_TRACK_DISTANCE = GetIniFloat("Circuit", "OnTrackDistance", ON_TRACK_DISTANCE);

        mapScreenPos = new PointF(posX, posY);
        mapScreenSize = new SizeF(Math.Max(1f, w), Math.Max(1f, h));
        pinSize = new SizeF(Math.Max(1f, pinW), Math.Max(1f, pinH));
    }

    private bool IsNearTrack(float x, float y)
    {
        float best = float.MaxValue;
        int count = TRACK_X.Length;
        for (int i = 0; i < count - 1; i++)
        {
            float d = DistancePointToSegment(x, y, TRACK_X[i], TRACK_Y[i], TRACK_X[i + 1], TRACK_Y[i + 1]);
            if (d < best) best = d;
            if (best <= ON_TRACK_DISTANCE) return true; // salida anticipada
        }
        return best <= ON_TRACK_DISTANCE;
    }

    private static float DistancePointToSegment(float px, float py, float ax, float ay, float bx, float by)
    {
        float abx = bx - ax, aby = by - ay;
        float apx = px - ax, apy = py - ay;
        float lenSq = abx * abx + aby * aby;
        float t = lenSq > 0.0001f ? (apx * abx + apy * aby) / lenSq : 0f;
        t = Math.Max(0f, Math.Min(1f, t));
        float cx = ax + abx * t;
        float cy = ay + aby * t;
        float dx = px - cx, dy = py - cy;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    private void OnTick(object sender, EventArgs e)
    {
        if (!enabled)
            return;

        var rawPos = Game.Player.Character.Position;
        float wx = rawPos.X - worldOffsetX;
        float wy = rawPos.Y - worldOffsetY;

        if (!IsNearTrack(wx, wy))
            return;

        mapSprite.Draw();

        // Mundo -> espacio de píxeles del lienzo 972x972, norte hacia arriba, sin rotación.
        float px = OFF_X + (wx - MIN_X) * SCALE;
        float py = CANVAS - OFF_Y - (wy - MIN_Y) * SCALE;

        // Escala del lienzo de origen 972x972 al tamaño del mapa en pantalla.
        float sx = mapScreenPos.X + (px / CANVAS) * mapScreenSize.Width;
        float sy = mapScreenPos.Y + (py / CANVAS) * mapScreenSize.Height;

        pinSprite.Position = new PointF(sx - pinSize.Width / 2f, sy - pinSize.Height / 2f);
        pinSprite.Draw();
    }
}
