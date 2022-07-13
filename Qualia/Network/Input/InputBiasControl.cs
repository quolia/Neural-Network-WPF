﻿using Qualia.Tools;
using System;

namespace Qualia.Controls
{
    sealed public class InputBiasControl : NeuronControl
    {
        public InputBiasControl()
            : base(0, null, null, null)
        {
            InitializeComponent();
        }

        public InputBiasControl(long id, Config config, Action<Notification.ParameterChanged> onChanged, LayerBaseControl parentLayer)
            : base(id, config, onChanged, parentLayer)
        {
            InitializeComponent();

            CtlIsBias.Value = true;
            CtlIsBiasConnected.Value = false;
            CtlIsBias.IsEnabled = false;
            CtlIsBiasConnected.SetVisible(CtlIsBias.Value);
            CtlIsBiasConnected.IsEnabled = false;
        }
    }
}
