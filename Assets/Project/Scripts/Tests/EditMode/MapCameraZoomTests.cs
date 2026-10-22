using System.Reflection;
using Fabula.Incrementant.AntFarm;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// Zoom em passos inteiros (<see cref="MapCamera"/>, etapa 1 do documento 40): de 16 px de tela por célula para cima o
    /// zoom só para em 16 e 32, a transição termina no valor exato, abaixo de 16 é contínuo e, parada num nível, a câmera
    /// fica alinhada à grade de pixels (com meio pixel de ajuste em resolução ímpar).
    /// </summary>
    public sealed class MapCameraZoomTests
    {
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        Camera _camera;
        RenderTexture _target;
        MapCamera _control;

        void Setup(int width, int height)
        {
            _target = new RenderTexture(width, height, 0);
            _camera = new GameObject("MapCameraZoomTests").AddComponent<Camera>();
            _camera.targetTexture = _target;
            _camera.transform.position = new Vector3(0f, 0f, -10f);
            _control = new MapCamera();
            // Mundo grande o bastante para o zoom mais aberto ficar bem abaixo de 16 px por célula.
            var bounds = new Rect(0f, 0f, 2000f, 1200f);
            _control.Reset(_camera, bounds, new Rect(400f, 300f, 640f, 400f), 10f, new Vector2(800.3f, 600.7f));
        }

        [TearDown]
        public void TearDown()
        {
            if (_camera != null)
                Object.DestroyImmediate(_camera.gameObject);
            if (_target != null)
                Object.DestroyImmediate(_target);
        }

        float PixelsPerCell => _camera.pixelHeight / (2f * _camera.orthographicSize);

        void Zoom(bool zoomIn)
        {
            var screen = new Vector2(_camera.pixelWidth * 0.37f, _camera.pixelHeight * 0.61f);
            typeof(MapCamera).GetMethod("ZoomAt", Private).Invoke(_control, new object[] { _camera, screen, zoomIn });
            // Avança a transição até o fim e aplica o alinhamento, como o Update faz a cada quadro.
            for (int i = 0; i < 30; i++)
                typeof(MapCamera).GetMethod("StepZoom", Private).Invoke(_control, new object[] { _camera, 0.02f });
            typeof(MapCamera).GetMethod("Clamp", Private).Invoke(_control, new object[] { _camera });
        }

        void AssertAlignedTo(int pixelsPerCell)
        {
            Assert.AreEqual(pixelsPerCell, PixelsPerCell, 1e-3f);
            Vector3 p = _camera.transform.position;
            float offsetX = (_camera.pixelWidth & 1) == 1 ? 0.5f : 0f;
            float offsetY = (_camera.pixelHeight & 1) == 1 ? 0.5f : 0f;
            float x = p.x * pixelsPerCell - offsetX, y = p.y * pixelsPerCell - offsetY;
            Assert.AreEqual(Mathf.Round(x), x, 1e-2f, "centro fora da grade de pixels em x");
            Assert.AreEqual(Mathf.Round(y), y, 1e-2f, "centro fora da grade de pixels em y");
        }

        [TestCase(1920, 1080)]
        [TestCase(1919, 1079)]
        public void ZoomStopsOnlyOnWholeLevelsAndStaysAligned(int width, int height)
        {
            Setup(width, height);
            AssertAlignedTo(16);   // começa em 16 px por célula

            Zoom(true);
            AssertAlignedTo(32);
            Zoom(true);
            AssertAlignedTo(32);   // máximo

            Zoom(false);
            AssertAlignedTo(16);
            Zoom(false);
            Assert.Less(PixelsPerCell, 16f - 0.5f, "abaixo do primeiro nível o zoom é contínuo");
            Zoom(false);
            float continuous = PixelsPerCell;
            Assert.Less(continuous, 16f);

            // Aproximando de novo, para em 16 exato (não passa direto para 32), mesmo vindo de perto dele.
            for (int i = 0; i < 6 && PixelsPerCell < 15.99f; i++)
                Zoom(true);
            AssertAlignedTo(16);
            Zoom(true);
            AssertAlignedTo(32);
        }
    }
}
