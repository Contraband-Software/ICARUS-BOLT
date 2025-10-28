using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public interface IPlayerInputHandler
    {
        void HandleJumpPressed();

        void HandleJumpReleased();

        void HandleSlide();

        void HandleSlideCancel();

        void HandleBoostPressed();
        void HandleBoostReleased();
    }
}
