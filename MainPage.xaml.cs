using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using Windows.Media.Protection;
using Windows.Media.Protection.PlayReady;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MSS_CSharp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            var protectionManager = new MediaProtectionManager();

            //A setting to tell MF that we are using PlayReady.
            var props = new Windows.Foundation.Collections.PropertySet();
            props.Add("{F4637010-03C3-42CD-B932-B48ADF3A6A54}", "Windows.Media.Protection.PlayReady.PlayReadyWinRTTrustedInput");
            protectionManager.Properties.Add("Windows.Media.Protection.MediaProtectionSystemIdMapping", props);
            protectionManager.Properties.Add("Windows.Media.Protection.MediaProtectionSystemId", "{F4637010-03C3-42CD-B932-B48ADF3A6A54}");

            //Maps the container guid from the manifest or media segment
            protectionManager.Properties.Add("Windows.Media.Protection.MediaProtectionContainerGuid", "{9A04F079-9840-4286-AB92-E65BE0885F95}");

            protectionManager.ServiceRequested += ProtectionManager_ServiceRequested;
            protectionManager.ComponentLoadFailed += ProtectionManager_ComponentLoadFailed;

            // media player
            MediaPlayer mediaPlayer = new MediaPlayer();
            mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;

            // media source
#if true
            AudioEncodingProperties audioProperties = AudioEncodingProperties.CreateAacAdts(44100, 1, 72000);
            Guid MF_SD_PROTECTED = new Guid(0xaf2181, 0xbdc2, 0x423c, 0xab, 0xca, 0xf5, 0x3, 0x59, 0x3b, 0xc1, 0x21);
            audioProperties.Properties.Add(MF_SD_PROTECTED, 1 /* true ? doc says UINT32 - treat as a boolean value */);
            AudioStreamDescriptor audioStreamDescriptor = new AudioStreamDescriptor(audioProperties);
            foreach (var prop in audioStreamDescriptor.EncodingProperties.Properties)
            {
                Log(prop.Key.ToString() + " => " + prop.Value.ToString());
            }
            MediaStreamSource mediaStreamSource = new MediaStreamSource(audioStreamDescriptor);
            mediaStreamSource.MediaProtectionManager = protectionManager;   // protection manager is on the media stream source
            mediaStreamSource.SampleRequested += MediaStreamSource_SampleRequested;
            mediaStreamSource.Starting += MediaStreamSource_Starting;
            MediaSource mediaSource = MediaSource.CreateFromMediaStreamSource(mediaStreamSource);
#else   
            MediaSource mediaSource = MediaSource.CreateFromUri(new Uri("http://profficialsite.origin.mediaservices.windows.net/c51358ea-9a5e-4322-8951-897d640fdfd7/tearsofsteel_4k.ism/manifest(format=mpd-time-csf)"));
            mediaPlayer.ProtectionManager = protectionManager;              // protection manager is on the media player
#endif

            // play !
            m_mediaPlayerElement.SetMediaPlayer(mediaPlayer);
            mediaPlayer.Source = mediaSource;
            mediaPlayer.Play();
        }

        private void MediaStreamSource_Starting(MediaStreamSource sender, MediaStreamSourceStartingEventArgs args)
        {
            Log("MediaStreamSource_Starting");
            args.Request.SetActualStartPosition(new TimeSpan(0));
        }

        private void MediaStreamSource_SampleRequested(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
        {
            Log("MediaStreamSource_SampleRequested");
            // no sample to provide...
        }

        private void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            Log("MediaPlayer_MediaOpened");
        }

        private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            Log(string.Format("MediaPlayer_MediaFailed: error = {0}, message = \"{1}\"", args.Error, args.ErrorMessage));
            Log(string.Format("MediaPlayer_MediaFailed: extended = {0}", args.ExtendedErrorCode));
            Log(string.Format("MediaPlayer_MediaFailed: hresult = {0:x}", args.ExtendedErrorCode.HResult));
        }

        private void ProtectionManager_ComponentLoadFailed(MediaProtectionManager sender, ComponentLoadFailedEventArgs e)
        {
            Log("ProtectionManager_ComponentLoadFailed");
        }

        private async void ProtectionManager_ServiceRequested(MediaProtectionManager sender, ServiceRequestedEventArgs e)
        {
            Log("ProtectionManager_ServiceRequested");

            if (e.Request is PlayReadyIndividualizationServiceRequest)
            {
                Log("ProtectionManager_ServiceRequested: PlayReadyIndividualizationServiceRequest");

                PlayReadyIndividualizationServiceRequest individualizationServiceRequest = (PlayReadyIndividualizationServiceRequest)e.Request;
                await individualizationServiceRequest.BeginServiceRequest();

                Log("ProtectionManager_ServiceRequested: PlayReadyIndividualizationServiceRequest complete");
                e.Completion.Complete(true);
            }
            else if (e.Request is PlayReadyLicenseAcquisitionServiceRequest)
            {
                Log("ProtectionManager_ServiceRequested: PlayReadyLicenseAcquisitionServiceRequest");

                PlayReadyLicenseAcquisitionServiceRequest licenseAcquisitionRequest = (PlayReadyLicenseAcquisitionServiceRequest)e.Request;
                await licenseAcquisitionRequest.BeginServiceRequest();

                Log("ProtectionManager_ServiceRequested: PlayReadyLicenseAcquisitionServiceRequest complete");
                e.Completion.Complete(true);
            }
        }

        private void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }
    }
}
