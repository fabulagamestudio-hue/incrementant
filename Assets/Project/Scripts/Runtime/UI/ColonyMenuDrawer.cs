using UnityEngine;
using UnityEngine.InputSystem;

namespace Fabula.Incrementant.UI
{
    /// <summary>
    /// Gaveta do menu Colônia: a coluna fica recuada para fora da borda esquerda, com só uma tira à mostra, e desliza para
    /// dentro quando o cursor chega perto dela. Quem chama decide quando ela precisa ficar aberta de qualquer jeito (submenu
    /// aberto, tutorial falando do menu). Sem mouse (toque, controle), fica sempre aberta.
    /// </summary>
    sealed class ColonyMenuDrawer
    {
        // Parte da coluna que fica à mostra recuada, em unidades do canvas.
        const float Peek = 36f;
        // Distância do cursor até a coluna aberta que a faz entrar; depois de aberta, só sai bem mais longe (histerese).
        const float OpenMargin = 48f, CloseMargin = 140f;
        // Velocidade do deslize (1/s); tempo real, para funcionar com o jogo pausado.
        const float Sharpness = 14f;

        readonly RectTransform _menu;
        readonly float _homeX;
        bool _open;

        public ColonyMenuDrawer(RectTransform menu)
        {
            _menu = menu;
            _homeX = menu.anchoredPosition.x;
        }

        /// <summary>A coluna está aberta (ou entrando).</summary>
        public bool Open => _open;

        // Borda direita da coluna a Peek da borda esquerda da tela (âncora à esquerda).
        float ClosedX => Peek - (1f - _menu.pivot.x) * _menu.rect.width;

        public void Tick(bool hold)
        {
            _open = hold || WantsOpen();
            Vector2 position = _menu.anchoredPosition;
            float target = _open ? _homeX : ClosedX;
            if (Mathf.Approximately(position.x, target))
                return;
            position.x = Mathf.Lerp(position.x, target, 1f - Mathf.Exp(-Sharpness * Time.unscaledDeltaTime));
            if (Mathf.Abs(position.x - target) < 0.5f)
                position.x = target;
            _menu.anchoredPosition = position;
        }

        bool WantsOpen()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
                return true;
            // Recuada, não entra no meio de um arrasto (pincel pintando perto da borda): espera o botão soltar.
            if (!_open && (mouse.leftButton.isPressed || mouse.rightButton.isPressed))
                return false;
            Vector2 screen = mouse.position.ReadValue();
            // Cursor fora da janela (no editor, numa aba ao lado da Game view): não conta como perto da borda.
            if (screen.x < 0f || screen.y < 0f || screen.x > Screen.width || screen.y > Screen.height)
                return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_menu, screen, null, out Vector2 local))
                return _open;
            // Ponto no espaço da coluna aberta: o espaço local anda junto com ela.
            local.x += _menu.anchoredPosition.x - _homeX;
            Rect rect = _menu.rect;
            float margin = _open ? CloseMargin : OpenMargin;
            // À esquerda não há limite: a borda da tela conta como perto.
            return local.x <= rect.xMax + margin && local.y >= rect.yMin - margin && local.y <= rect.yMax + margin;
        }
    }
}
