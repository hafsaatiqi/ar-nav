using UnityEngine;
using System;
using ZXing;
using UnityEngine.UI; // for RawImage

public class QRScanner : MonoBehaviour {
    private WebCamTexture camTex;
    private IBarcodeReader reader;
    public event Action<string> OnQRScanned;

    // Optional: drag the RawImage here in inspector to display the camera feed
    public RawImage rawImageDisplay;

    void Start() {
        camTex = new WebCamTexture();
        reader = new BarcodeReader();

        if (rawImageDisplay != null) {
            rawImageDisplay.texture = camTex;
        } else {
            // fallback: set texture to renderer if present (quad)
            var renderer = GetComponent<Renderer>();
            if (renderer != null) renderer.material.mainTexture = camTex;
        }

        camTex.Play();
        InvokeRepeating(nameof(Decode), 0.5f, 0.5f);
    }

    void Decode() {
        try {
            if (camTex.width <= 16 || camTex.height <= 16) return; // camera not ready
            var snap = new Texture2D(camTex.width, camTex.height, TextureFormat.RGBA32, false);
            snap.SetPixels32(camTex.GetPixels32());
            snap.Apply();
            var result = reader.Decode(snap.GetPixels32(), snap.width, snap.height);
            if(result != null) {
                Debug.Log("QR: " + result.Text);
                OnQRScanned?.Invoke(result.Text);
            }
            Destroy(snap);
        } catch(Exception e) {
            Debug.LogWarning("Decode error: " + e.Message);
        }
    }
}
