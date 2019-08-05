﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tools;

namespace Qualia.Controls
{
    /// <summary>
    /// Interaction logic for OutputNeuronControl.xaml
    /// </summary>
    public partial class OutputNeuronControl : NeuronBase
    {
        /*
        public OutputNeuronControl()
        {
            InitializeComponent();
        }
        */
        public OutputNeuronControl(long id, Config config, Action<Notification.ParameterChanged, object> onNetworkUIChanged)
            : base(id, config, onNetworkUIChanged)
        {
            InitializeComponent();

            LoadConfig();

            CtlActivationFunc.SelectedIndexChanged += CtlActivationFunc_SelectedIndexChanged;
            CtlActivationFuncParamA.Changed += OnChanged;
        }

        public override string WeightsInitializer => nameof(InitializeMode.None);
        public override double? WeightsInitializerParamA => null;
        public override bool IsBias => false;
        public override bool IsBiasConnected => false;
        public override string ActivationFunc => CtlActivationFunc.SelectedItem.ToString();
        public override double? ActivationFuncParamA => CtlActivationFuncParamA.ValueOrNull;

        public void LoadConfig()
        {
            ActivationFunction.Helper.FillComboBox(CtlActivationFunc, Config, Const.Param.ActivationFunc, nameof(ActivationFunction.LogisticSigmoid));
            CtlActivationFuncParamA.Load(Config);

            StateChanged();
        }

        private void CtlActivationFunc_SelectedIndexChanged(int index)
        {
            //CtlActivationFuncParamALabel.Focus();
            OnNetworkUIChanged(Notification.ParameterChanged.Structure, false);
        }

        private void OnChanged()
        {
            OnNetworkUIChanged(Notification.ParameterChanged.Structure, null);
        }

        public override void OrdinalNumberChanged(int number)
        {
            CtlNumber.Content = number.ToString();
        }

        public override bool IsValid()
        {
            return CtlActivationFuncParamA.IsValid();
        }

        public override void SaveConfig()
        {
            Config.Set(Const.Param.ActivationFunc, CtlActivationFunc.SelectedItem.ToString());
            CtlActivationFuncParamA.Save(Config);
        }

        public override void VanishConfig()
        {
            Config.Remove(Const.Param.ActivationFunc);
            CtlActivationFuncParamA.Vanish(Config);
        }
    }
}
