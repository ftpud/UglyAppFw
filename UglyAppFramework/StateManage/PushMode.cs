namespace UglyAppFramework.StateManage;

// How a pushed state relates to the visible states below it.
public enum PushMode
{
    // Standard flow: the pushed state replaces the screen; everything visible is hidden
    // and shown again when it is popped.
    Replace,

    // A separate panel shown after the visible states, which stay visible and interactive.
    // It has its own lifecycle (pop closes just it and anything above it) and does not
    // take typed text, which keeps going to the page below.
    Panel,

    // A blocking dialog shown after the visible states: they stay visible but ignore
    // input until it is popped. Typed text goes to the modal.
    Modal
}
