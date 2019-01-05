﻿namespace SplashScreen
{
    using System;

    /// <summary>
    /// Add this attribute to the WPF control that holds the design for the splash screen.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SplashScreenAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the default fadeout-duration that is used to fade out the splash screen after the applications main window has been loaded.
        /// </summary>
        public TimeSpan FadeoutDuration { get; set; } = TimeSpan.FromSeconds(1);
        /// <summary>
        /// Gets or sets the minimum-visibility duration of the splash screen. The splash will be kept visible at least this time, even if the main window is loaded earlier.
        /// </summary>
        public TimeSpan MinimumVisibilityDuration { get; set; } = TimeSpan.FromSeconds(4);
    }
}