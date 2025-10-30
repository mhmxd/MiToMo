using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Panel.Nav
{
    internal class Sounder
    {

        private static SoundPlayer _hitSound = new SoundPlayer(Panel.Nav.Properties.Resources.hit);
        private static SoundPlayer _startMiss = new SoundPlayer(Panel.Nav.Properties.Resources.start_miss);
        private static SoundPlayer _targetMiss = new SoundPlayer(Panel.Nav.Properties.Resources.target_miss);

        static Sounder() {
            _hitSound.Load();
            _startMiss.Load();
            _targetMiss.Load();
        }

        public static void PlayHit() 
        {
            _hitSound.Play(); 
        }
        public static void PlayStartMiss() 
        { 
            _startMiss.Play(); 
        }
        public static void PlayTargetMiss() 
        {
            _targetMiss.Play(); 
        }
    }
}
