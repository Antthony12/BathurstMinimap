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
    private const float MIN_X = -3960.2700000f;
    private const float MIN_Y = 7907.1400000f;
    private const float SCALE = 0.4165265793f;
    private const float OFF_X = 242.7214035f;
    private const float OFF_Y = 40.0000000f;

    // Telemetría real de una vuelta grabada (~674 puntos, uno cada ~9m), usada
    // solo para detectar si el jugador está cerca del circuito. Incrustada
    // directamente aquí para no necesitar ningún archivo de datos aparte.
    private static readonly float[] TRACK_X = {
            -3001.38f, -3010.48f, -3019.69f, -3028.98f, -3038.41f, -3047.48f, -3056.65f, -3065.88f, -3075.30f, -3084.49f,
            -3093.81f, -3103.22f, -3112.61f, -3122.00f, -3131.46f, -3140.46f, -3149.48f, -3158.61f, -3167.76f, -3176.98f,
            -3186.29f, -3195.62f, -3205.01f, -3214.42f, -3223.89f, -3233.42f, -3242.94f, -3252.47f, -3262.01f, -3271.02f,
            -3280.06f, -3289.10f, -3298.13f, -3307.55f, -3316.55f, -3325.62f, -3334.97f, -3344.12f, -3353.22f, -3362.19f,
            -3370.55f, -3377.18f, -3381.82f, -3384.09f, -3384.57f, -3384.67f, -3384.75f, -3384.83f, -3384.92f, -3385.04f,
            -3385.17f, -3385.29f, -3385.39f, -3385.51f, -3385.64f, -3385.77f, -3385.90f, -3386.03f, -3386.15f, -3386.28f,
            -3386.49f, -3386.73f, -3386.99f, -3387.19f, -3387.36f, -3387.63f, -3387.94f, -3388.22f, -3388.35f, -3388.39f,
            -3388.44f, -3388.53f, -3388.64f, -3388.75f, -3388.90f, -3389.09f, -3389.31f, -3389.53f, -3389.72f, -3389.92f,
            -3390.12f, -3390.31f, -3390.52f, -3390.73f, -3390.95f, -3391.05f, -3391.02f, -3390.93f, -3390.83f, -3390.73f,
            -3390.64f, -3390.61f, -3390.69f, -3391.03f, -3391.40f, -3391.60f, -3391.62f, -3391.58f, -3391.51f, -3391.44f,
            -3391.41f, -3391.43f, -3391.61f, -3392.06f, -3392.39f, -3392.48f, -3392.47f, -3392.54f, -3392.73f, -3392.99f,
            -3393.30f, -3393.63f, -3393.97f, -3394.29f, -3394.49f, -3394.59f, -3394.67f, -3394.74f, -3394.80f, -3394.88f,
            -3394.95f, -3395.02f, -3395.10f, -3395.18f, -3395.24f, -3395.32f, -3395.41f, -3395.51f, -3395.63f, -3395.76f,
            -3395.87f, -3396.02f, -3396.24f, -3396.51f, -3396.80f, -3397.08f, -3397.36f, -3397.63f, -3397.90f, -3398.16f,
            -3398.38f, -3398.51f, -3398.65f, -3398.81f, -3398.96f, -3399.12f, -3399.24f, -3399.22f, -3399.10f, -3398.94f,
            -3398.91f, -3399.03f, -3399.16f, -3399.24f, -3399.30f, -3399.32f, -3399.34f, -3399.39f, -3399.55f, -3400.23f,
            -3401.69f, -3403.99f, -3407.92f, -3413.33f, -3419.47f, -3426.38f, -3433.70f, -3441.72f, -3450.01f, -3459.04f,
            -3468.05f, -3477.25f, -3486.26f, -3495.56f, -3504.52f, -3513.41f, -3522.39f, -3531.11f, -3539.78f, -3548.43f,
            -3557.14f, -3565.54f, -3573.96f, -3582.48f, -3591.13f, -3599.88f, -3608.64f, -3617.46f, -3626.33f, -3635.20f,
            -3644.07f, -3652.55f, -3661.06f, -3669.70f, -3678.36f, -3687.02f, -3695.69f, -3704.37f, -3712.97f, -3721.87f,
            -3730.39f, -3739.01f, -3747.61f, -3756.58f, -3765.52f, -3774.52f, -3783.85f, -3792.86f, -3801.74f, -3810.64f,
            -3819.16f, -3827.50f, -3835.50f, -3843.50f, -3851.29f, -3859.04f, -3865.16f, -3868.14f, -3868.92f, -3867.42f,
            -3863.69f, -3858.79f, -3852.25f, -3845.48f, -3838.61f, -3831.73f, -3824.93f, -3817.97f, -3810.94f, -3804.21f,
            -3797.71f, -3791.64f, -3786.31f, -3781.83f, -3778.45f, -3775.92f, -3774.26f, -3773.24f, -3772.40f, -3771.95f,
            -3771.91f, -3771.82f, -3771.67f, -3771.57f, -3771.62f, -3771.76f, -3771.94f, -3772.15f, -3772.52f, -3773.56f,
            -3775.52f, -3777.78f, -3780.83f, -3785.00f, -3790.37f, -3796.73f, -3803.94f, -3811.38f, -3819.08f, -3826.98f,
            -3834.62f, -3842.21f, -3849.56f, -3856.62f, -3862.94f, -3868.87f, -3874.40f, -3879.81f, -3885.35f, -3890.74f,
            -3896.17f, -3901.49f, -3906.75f, -3912.23f, -3917.54f, -3922.67f, -3927.46f, -3932.16f, -3936.49f, -3940.33f,
            -3943.73f, -3946.35f, -3948.29f, -3949.68f, -3951.11f, -3952.89f, -3954.80f, -3956.50f, -3958.08f, -3959.47f,
            -3960.25f, -3960.27f, -3959.66f, -3958.38f, -3956.65f, -3954.47f, -3951.31f, -3946.97f, -3941.83f, -3936.86f,
            -3931.70f, -3926.04f, -3919.90f, -3913.60f, -3907.35f, -3901.47f, -3895.61f, -3889.69f, -3883.75f, -3877.70f,
            -3871.73f, -3865.72f, -3859.67f, -3853.54f, -3847.39f, -3841.23f, -3835.34f, -3829.34f, -3823.14f, -3816.63f,
            -3809.93f, -3802.86f, -3795.33f, -3787.49f, -3779.49f, -3770.91f, -3762.24f, -3753.37f, -3744.29f, -3735.03f,
            -3725.68f, -3716.29f, -3706.90f, -3697.49f, -3688.05f, -3679.06f, -3669.91f, -3660.72f, -3651.42f, -3642.19f,
            -3632.90f, -3623.59f, -3614.24f, -3604.79f, -3595.31f, -3586.36f, -3576.80f, -3567.27f, -3558.29f, -3548.80f,
            -3539.48f, -3530.16f, -3521.10f, -3512.06f, -3502.97f, -3493.77f, -3484.60f, -3475.54f, -3466.42f, -3457.29f,
            -3448.29f, -3439.43f, -3430.93f, -3422.52f, -3414.07f, -3405.31f, -3396.26f, -3387.08f, -3377.98f, -3369.23f,
            -3360.73f, -3352.18f, -3343.24f, -3334.29f, -3325.52f, -3317.27f, -3309.53f, -3302.33f, -3295.92f, -3290.59f,
            -3286.09f, -3280.34f, -3272.07f, -3263.11f, -3254.11f, -3244.93f, -3235.91f, -3226.87f, -3217.90f, -3209.38f,
            -3201.33f, -3193.44f, -3185.94f, -3178.24f, -3170.73f, -3163.08f, -3155.41f, -3147.47f, -3139.38f, -3131.52f,
            -3123.43f, -3115.14f, -3106.41f, -3097.40f, -3088.25f, -3079.08f, -3070.04f, -3061.34f, -3053.12f, -3045.25f,
            -3038.24f, -3032.21f, -3026.55f, -3020.83f, -3015.30f, -3009.69f, -3004.22f, -2999.02f, -2993.80f, -2988.06f,
            -2981.13f, -2973.33f, -2964.57f, -2955.81f, -2947.38f, -2939.89f, -2934.05f, -2929.35f, -2925.30f, -2921.55f,
            -2917.78f, -2913.94f, -2909.99f, -2906.17f, -2902.49f, -2899.09f, -2895.82f, -2892.54f, -2889.27f, -2885.92f,
            -2882.65f, -2879.30f, -2875.87f, -2872.56f, -2869.20f, -2866.12f, -2863.46f, -2861.56f, -2860.32f, -2859.53f,
            -2859.07f, -2858.78f, -2858.60f, -2858.47f, -2858.49f, -2858.64f, -2858.78f, -2858.87f, -2858.95f, -2859.01f,
            -2859.04f, -2859.05f, -2859.06f, -2858.96f, -2858.64f, -2858.34f, -2858.17f, -2858.05f, -2857.93f, -2857.82f,
            -2857.71f, -2857.64f, -2857.65f, -2857.69f, -2857.75f, -2857.81f, -2857.88f, -2857.95f, -2858.03f, -2858.11f,
            -2858.22f, -2858.36f, -2858.51f, -2858.66f, -2858.82f, -2858.95f, -2859.08f, -2859.20f, -2859.32f, -2859.43f,
            -2859.55f, -2859.70f, -2859.92f, -2860.18f, -2860.46f, -2860.73f, -2860.99f, -2861.24f, -2861.43f, -2861.55f,
            -2861.66f, -2861.74f, -2861.79f, -2861.83f, -2861.83f, -2861.81f, -2861.78f, -2861.83f, -2861.96f, -2862.14f,
            -2862.34f, -2862.53f, -2862.72f, -2862.87f, -2862.96f, -2862.97f, -2862.91f, -2862.86f, -2862.86f, -2862.87f,
            -2862.91f, -2863.01f, -2863.14f, -2863.28f, -2863.41f, -2863.52f, -2863.60f, -2863.62f, -2863.58f, -2863.52f,
            -2863.46f, -2863.40f, -2863.41f, -2863.47f, -2863.56f, -2863.65f, -2863.74f, -2863.83f, -2863.90f, -2863.96f,
            -2864.01f, -2864.05f, -2864.09f, -2864.12f, -2864.14f, -2864.19f, -2864.29f, -2864.39f, -2864.48f, -2864.57f,
            -2864.66f, -2864.74f, -2864.81f, -2864.83f, -2864.81f, -2864.76f, -2864.70f, -2864.70f, -2864.82f, -2865.20f,
            -2865.75f, -2866.24f, -2866.60f, -2866.85f, -2866.87f, -2866.46f, -2865.50f, -2864.07f, -2862.13f, -2859.78f,
            -2857.26f, -2854.83f, -2852.57f, -2850.32f, -2847.91f, -2845.23f, -2842.35f, -2839.40f, -2836.50f, -2833.80f,
            -2831.32f, -2828.99f, -2826.70f, -2824.34f, -2821.83f, -2819.05f, -2816.27f, -2813.50f, -2810.60f, -2807.85f,
            -2804.98f, -2802.21f, -2799.42f, -2796.76f, -2794.60f, -2792.93f, -2792.14f, -2793.62f, -2797.77f, -2803.91f,
            -2811.00f, -2818.84f, -2826.30f, -2833.18f, -2839.99f, -2846.39f, -2852.07f, -2856.87f, -2861.27f, -2864.86f,
            -2867.57f, -2868.74f, -2869.18f, -2869.82f, -2870.21f, -2870.08f, -2869.79f, -2869.70f, -2870.04f, -2870.53f,
            -2870.71f, -2870.66f, -2870.60f, -2870.62f, -2870.71f, -2870.83f, -2870.95f, -2871.06f, -2871.18f, -2871.29f,
            -2871.41f, -2871.53f, -2871.65f, -2871.76f, -2871.86f, -2871.95f, -2872.05f, -2872.15f, -2872.25f, -2872.34f,
            -2872.40f, -2872.42f, -2872.38f, -2872.32f, -2872.26f, -2872.19f, -2872.13f, -2872.07f, -2872.01f, -2871.96f,
            -2871.94f, -2871.94f, -2871.96f, -2871.97f, -2871.99f, -2872.02f, -2872.08f, -2872.94f, -2875.72f, -2881.19f,
            -2888.69f, -2897.36f, -2906.68f, -2915.86f, -2925.02f, -2934.07f, -2943.36f, -2952.63f, -2961.79f, -2971.15f,
            -2980.29f, -2989.42f, -2998.68f, -3001.38f,
    };

    private static readonly float[] TRACK_Y = {
            10045.60f, 10045.83f, 10045.98f, 10046.05f, 10046.09f, 10046.14f, 10046.19f, 10046.24f, 10046.30f, 10046.41f,
            10046.62f, 10046.84f, 10046.86f, 10046.55f, 10046.32f, 10046.32f, 10046.44f, 10046.61f, 10046.80f, 10046.99f,
            10047.18f, 10047.36f, 10047.54f, 10047.69f, 10047.81f, 10047.87f, 10047.86f, 10047.79f, 10047.69f, 10047.65f,
            10047.76f, 10047.97f, 10048.19f, 10048.32f, 10048.33f, 10048.29f, 10048.46f, 10048.66f, 10048.26f, 10046.88f,
            10043.16f, 10036.99f, 10029.15f, 10020.17f, 10011.04f, 10001.77f, 9992.42f, 9983.08f, 9973.77f, 9964.56f,
            9955.54f, 9946.30f, 9937.24f, 9928.09f, 9918.96f, 9909.76f, 9900.42f, 9891.35f, 9882.09f, 9872.72f,
            9863.25f, 9854.14f, 9844.88f, 9835.53f, 9826.43f, 9817.04f, 9807.53f, 9798.51f, 9789.47f, 9780.28f,
            9770.96f, 9761.61f, 9752.19f, 9742.77f, 9733.41f, 9724.02f, 9714.59f, 9705.09f, 9696.09f, 9686.54f,
            9677.47f, 9668.35f, 9659.22f, 9650.00f, 9640.74f, 9631.51f, 9622.00f, 9612.70f, 9603.46f, 9594.21f,
            9584.94f, 9575.60f, 9566.19f, 9556.72f, 9547.71f, 9538.69f, 9529.65f, 9520.52f, 9511.39f, 9502.25f,
            9493.11f, 9483.90f, 9474.63f, 9465.35f, 9456.01f, 9446.61f, 9437.21f, 9427.80f, 9418.35f, 9408.83f,
            9399.35f, 9389.79f, 9380.22f, 9370.63f, 9361.56f, 9352.43f, 9343.23f, 9334.06f, 9324.90f, 9315.71f,
            9306.50f, 9297.26f, 9287.98f, 9278.74f, 9269.69f, 9260.21f, 9250.89f, 9241.81f, 9232.78f, 9223.26f,
            9214.24f, 9204.72f, 9195.72f, 9186.20f, 9176.69f, 9167.63f, 9158.61f, 9149.56f, 9140.49f, 9131.36f,
            9122.12f, 9112.82f, 9103.36f, 9093.85f, 9084.81f, 9075.63f, 9066.51f, 9057.33f, 9048.15f, 9039.03f,
            9029.87f, 9020.81f, 9011.29f, 9002.07f, 8993.07f, 8983.92f, 8974.80f, 8965.65f, 8956.36f, 8947.22f,
            8938.24f, 8929.34f, 8921.01f, 8913.41f, 8906.82f, 8900.72f, 8895.37f, 8890.67f, 8887.00f, 8884.57f,
            8883.70f, 8883.90f, 8884.76f, 8886.26f, 8888.15f, 8890.28f, 8892.53f, 8895.14f, 8898.27f, 8901.62f,
            8905.01f, 8908.26f, 8911.52f, 8914.84f, 8918.24f, 8921.63f, 8924.88f, 8928.10f, 8931.33f, 8934.54f,
            8937.73f, 8940.77f, 8943.81f, 8946.87f, 8949.93f, 8952.98f, 8956.03f, 8959.08f, 8962.09f, 8965.21f,
            8968.20f, 8971.15f, 8973.85f, 8976.33f, 8978.26f, 8978.88f, 8978.21f, 8977.02f, 8975.21f, 8972.48f,
            8969.31f, 8965.62f, 8961.44f, 8956.85f, 8952.25f, 8947.43f, 8940.87f, 8932.29f, 8923.16f, 8914.31f,
            8905.91f, 8898.37f, 8891.81f, 8885.77f, 8879.70f, 8873.65f, 8867.69f, 8861.62f, 8855.55f, 8849.53f,
            8842.83f, 8835.92f, 8828.66f, 8820.61f, 8812.05f, 8803.01f, 8794.03f, 8784.98f, 8775.87f, 8766.59f,
            8757.16f, 8748.10f, 8738.93f, 8729.63f, 8720.33f, 8710.93f, 8701.64f, 8692.30f, 8683.00f, 8673.87f,
            8664.94f, 8655.95f, 8647.13f, 8639.16f, 8631.78f, 8625.15f, 8619.36f, 8614.20f, 8609.09f, 8603.99f,
            8599.08f, 8593.79f, 8587.99f, 8581.70f, 8575.14f, 8568.15f, 8560.73f, 8553.10f, 8545.46f, 8538.14f,
            8530.77f, 8523.20f, 8515.42f, 8507.69f, 8500.35f, 8492.75f, 8484.96f, 8476.79f, 8468.59f, 8460.15f,
            8451.31f, 8442.53f, 8433.39f, 8424.03f, 8415.11f, 8406.11f, 8396.99f, 8387.73f, 8378.37f, 8369.02f,
            8359.83f, 8350.83f, 8341.54f, 8332.41f, 8323.34f, 8314.60f, 8305.79f, 8297.60f, 8289.76f, 8282.18f,
            8274.61f, 8267.36f, 8260.38f, 8253.42f, 8246.25f, 8239.36f, 8232.47f, 8225.52f, 8218.56f, 8211.52f,
            8204.59f, 8197.63f, 8190.61f, 8183.51f, 8176.37f, 8169.19f, 8162.36f, 8155.46f, 8148.67f, 8142.00f,
            8135.53f, 8129.33f, 8123.71f, 8118.75f, 8114.34f, 8110.33f, 8106.92f, 8103.99f, 8101.67f, 8100.03f,
            8099.15f, 8099.24f, 8100.34f, 8101.94f, 8103.33f, 8104.45f, 8105.55f, 8106.73f, 8108.17f, 8109.75f,
            8111.30f, 8112.76f, 8114.14f, 8115.52f, 8116.91f, 8118.22f, 8119.61f, 8120.99f, 8122.30f, 8123.70f,
            8125.07f, 8126.45f, 8127.78f, 8129.11f, 8130.45f, 8131.52f, 8131.97f, 8132.23f, 8132.06f, 8130.80f,
            8128.63f, 8125.99f, 8122.62f, 8118.97f, 8115.35f, 8112.33f, 8110.80f, 8110.83f, 8112.26f, 8114.79f,
            8118.03f, 8121.34f, 8123.73f, 8124.63f, 8122.81f, 8118.97f, 8114.27f, 8108.85f, 8102.36f, 8094.87f,
            8086.87f, 8079.97f, 8076.19f, 8074.92f, 8075.70f, 8076.85f, 8077.85f, 8077.78f, 8076.13f, 8072.98f,
            8068.39f, 8063.27f, 8058.20f, 8052.80f, 8047.52f, 8042.25f, 8037.30f, 8032.49f, 8027.65f, 8022.96f,
            8018.31f, 8014.63f, 8011.97f, 8009.90f, 8008.12f, 8006.46f, 8004.21f, 8001.09f, 7996.99f, 7991.94f,
            7986.00f, 7979.09f, 7971.86f, 7964.43f, 7957.10f, 7949.64f, 7942.36f, 7934.84f, 7927.24f, 7920.25f,
            7914.14f, 7909.43f, 7907.14f, 7908.96f, 7912.06f, 7917.08f, 7923.96f, 7931.90f, 7940.16f, 7948.43f,
            7956.77f, 7965.11f, 7973.59f, 7982.12f, 7990.75f, 7999.37f, 8007.86f, 8016.41f, 8024.87f, 8033.52f,
            8041.92f, 8050.54f, 8059.39f, 8067.93f, 8076.67f, 8085.15f, 8093.95f, 8102.91f, 8112.11f, 8121.43f,
            8130.79f, 8140.34f, 8149.41f, 8158.66f, 8168.03f, 8177.60f, 8187.17f, 8196.26f, 8205.42f, 8214.68f,
            8224.09f, 8233.50f, 8243.02f, 8252.06f, 8261.12f, 8270.15f, 8279.78f, 8288.80f, 8297.89f, 8307.01f,
            8316.23f, 8325.50f, 8334.73f, 8344.01f, 8353.34f, 8362.73f, 8372.25f, 8381.69f, 8391.11f, 8400.58f,
            8410.05f, 8419.60f, 8429.23f, 8438.79f, 8448.44f, 8458.03f, 8467.57f, 8477.16f, 8486.69f, 8496.25f,
            8505.83f, 8515.40f, 8525.00f, 8534.59f, 8544.17f, 8553.79f, 8563.40f, 8573.02f, 8582.68f, 8592.26f,
            8601.87f, 8611.47f, 8621.11f, 8630.14f, 8639.15f, 8648.18f, 8657.21f, 8666.23f, 8675.27f, 8684.32f,
            8693.93f, 8703.52f, 8713.14f, 8722.71f, 8732.34f, 8741.93f, 8751.52f, 8761.14f, 8770.76f, 8780.44f,
            8789.52f, 8799.18f, 8808.18f, 8817.84f, 8826.85f, 8835.90f, 8844.95f, 8853.99f, 8863.01f, 8872.68f,
            8882.35f, 8891.37f, 8901.05f, 8910.06f, 8919.11f, 8928.18f, 8937.28f, 8946.37f, 8955.44f, 8964.56f,
            8973.64f, 8982.72f, 8991.85f, 9000.98f, 9010.15f, 9019.27f, 9028.42f, 9037.61f, 9046.83f, 9056.03f,
            9065.15f, 9074.26f, 9083.49f, 9092.79f, 9102.20f, 9111.63f, 9121.07f, 9130.53f, 9140.07f, 9149.54f,
            9159.11f, 9168.58f, 9178.17f, 9187.73f, 9197.31f, 9206.88f, 9216.33f, 9225.65f, 9234.93f, 9244.08f,
            9253.15f, 9262.27f, 9271.43f, 9280.52f, 9289.58f, 9298.54f, 9307.47f, 9316.42f, 9325.40f, 9334.37f,
            9343.46f, 9352.54f, 9361.55f, 9370.42f, 9379.12f, 9388.23f, 9397.15f, 9405.96f, 9415.15f, 9423.82f,
            9432.89f, 9441.61f, 9450.50f, 9459.24f, 9468.08f, 9477.04f, 9486.05f, 9494.90f, 9502.86f, 9509.73f,
            9515.32f, 9520.44f, 9525.98f, 9531.91f, 9538.26f, 9544.73f, 9552.00f, 9559.76f, 9567.93f, 9576.28f,
            9585.30f, 9594.29f, 9603.61f, 9612.59f, 9621.75f, 9631.05f, 9640.42f, 9649.89f, 9658.95f, 9668.06f,
            9677.26f, 9686.52f, 9695.91f, 9705.32f, 9714.73f, 9724.10f, 9733.55f, 9742.56f, 9751.59f, 9760.74f,
            9769.94f, 9779.31f, 9788.84f, 9798.37f, 9807.91f, 9816.92f, 9826.00f, 9835.10f, 9844.23f, 9853.44f,
            9862.76f, 9872.20f, 9881.64f, 9891.10f, 9900.63f, 9910.20f, 9919.20f, 9928.20f, 9937.24f, 9946.34f,
            9955.41f, 9965.00f, 9974.39f, 9983.90f, 9993.06f, 10002.26f, 10011.40f, 10020.37f, 10029.03f, 10036.17f,
            10041.38f, 10043.62f, 10043.68f, 10043.42f, 10043.34f, 10043.70f, 10044.19f, 10044.45f, 10044.61f, 10044.77f,
            10044.95f, 10045.16f, 10045.36f, 10045.60f,
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
