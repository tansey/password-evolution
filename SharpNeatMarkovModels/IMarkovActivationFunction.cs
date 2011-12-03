using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpNeat.Network;

namespace SharpNeatMarkovModels
{
    public interface IMarkovActivationFunction : IActivationFunction
    {
        string State { get; }
    }
}
