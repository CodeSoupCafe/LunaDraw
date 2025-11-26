namespace LunaDraw.Logic.Utils
{
    public enum TouchManipulationMode
    {
        None = 0,
        PanOnly = 1,
        IsotropicScale = 2,     // includes panning
        AnisotropicScale = 3,   // includes panning
        ScaleRotate = 4,        // implies isotropic scaling
        ScaleDualRotate = 5    // adds one-finger rotation
    }
}
