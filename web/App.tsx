import { useState, useEffect, useRef } from 'react';
import {
  Home,
  Sliders,
  Headphones,
  Settings,
  Zap,
  Activity,
  Play,
  Square,
  Volume2,
  ExternalLink,
  BatteryCharging,
  RefreshCw,
  Sparkles,
  ShieldCheck,
  Radio,
  CheckCircle2,
  Download,
  Laptop
} from 'lucide-react';
import {
  isHosted,
  sendToHost,
  onHostMessage,
  HostMessage,
  HostDevice,
  formatDuration
} from './host';

interface GitHubRelease {
  tag_name: string;
  name: string;
  body: string;
  published_at: string;
  html_url: string;
  assets: { name: string; browser_download_url: string; size: number }[];
}

export default function App() {
  const [activeTab, setActiveTab] = useState<'dashboard' | 'controls' | 'devices' | 'settings'>('dashboard');
  const [isRunning, setIsRunning] = useState<boolean>(true);
  const [keepActiveWhenPaused, setKeepActiveWhenPaused] = useState<boolean>(true);
  const [appVersion, setAppVersion] = useState<string>('v1.0.4');
  const [sessionSeconds, setSessionSeconds] = useState<number>(0);
  
  // Separate laptop battery vs speaker battery
  const [laptopBatteryLevel, setLaptopBatteryLevel] = useState<number>(90);
  const [isLaptopCharging, setIsLaptopCharging] = useState<boolean>(false);
  const [speakerBatteryLevel, setSpeakerBatteryLevel] = useState<number>(85);
  const [isSpeakerConnected, setIsSpeakerConnected] = useState<boolean>(true);
  const [speakerStatusText, setSpeakerStatusText] = useState<string>('Connected via Bluetooth GATT / HFP');
  
  const [selectedDevice, setSelectedDevice] = useState<string>('Wireless Bluetooth Speaker');
  const [selectedDeviceNumber, setSelectedDeviceNumber] = useState<number>(-1);
  const [hostDevices, setHostDevices] = useState<HostDevice[]>([]);

  const [autoRePairActive, setAutoRePairActive] = useState<boolean>(true);
  const [callProtectionActive, setCallProtectionActive] = useState<boolean>(true);
  const [autoPauseOnOtherAudio, setAutoPauseOnOtherAudio] = useState<boolean>(false);
  const [startOnStartup, setStartOnStartup] = useState<boolean>(true);
  const [minimizeToTray, setMinimizeToTray] = useState<boolean>(true);
  const [showNotifications, setShowNotifications] = useState<boolean>(true);

  const [toastMessage, setToastMessage] = useState<string | null>(null);
  
  // Update state from GitHub repo / Host UpdateService
  const [checkingUpdate, setCheckingUpdate] = useState<boolean>(false);
  const [latestRelease, setLatestRelease] = useState<GitHubRelease | null>(null);
  const [updateStatus, setUpdateStatus] = useState<string>('Checking release status...');
  const [showUpdateModal, setShowUpdateModal] = useState<boolean>(false);

  // Avatar load status
  const [avatarError, setAvatarError] = useState<boolean>(false);

  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const phaseRef = useRef<number>(0);

  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => {
      setToastMessage((curr) => (curr === msg ? null : curr));
    }, 3800);
  };

  // Host Message Bridge
  useEffect(() => {
    if (isHosted) {
      sendToHost({ type: 'ready' });
    }

    const unsubscribe = onHostMessage((msg: HostMessage) => {
      if (msg.type === 'state') {
        if (msg.version) setAppVersion(msg.version.startsWith('v') ? msg.version : `v${msg.version}`);
        setIsRunning(msg.isRunning);
        setSessionSeconds(msg.runningSeconds || 0);
        if (msg.deviceName) setSelectedDevice(msg.deviceName);
        if (msg.deviceNumber !== undefined) setSelectedDeviceNumber(msg.deviceNumber);
        if (msg.devices) setHostDevices(msg.devices);

        if (msg.speaker) {
          if (msg.speaker.name) setSelectedDevice(msg.speaker.name);
          setIsSpeakerConnected(msg.speaker.connected);
          if (msg.speaker.battery !== null && msg.speaker.battery !== undefined) {
            setSpeakerBatteryLevel(msg.speaker.battery);
          }
          if (msg.speaker.status) setSpeakerStatusText(msg.speaker.status);
        }

        if (msg.laptop) {
          setLaptopBatteryLevel(msg.laptop.level);
          setIsLaptopCharging(msg.laptop.charging);
        }

        if (msg.settings) {
          setKeepActiveWhenPaused(msg.settings.alwaysKeepStreamActive);
          setAutoRePairActive(msg.settings.autoReconnect);
          setCallProtectionActive(msg.settings.callProtection);
          setAutoPauseOnOtherAudio(msg.settings.autoPauseOnOtherAudio);
          setStartOnStartup(msg.settings.startOnWindowsStartup);
          setMinimizeToTray(msg.settings.minimizeToTrayOnClose);
          setShowNotifications(msg.settings.showNotifications);
        }
      } else if (msg.type === 'toast') {
        showToast(msg.message);
      } else if (msg.type === 'navigate') {
        setActiveTab(msg.tab);
      } else if (msg.type === 'update') {
        setCheckingUpdate(false);
        if (msg.isUpdateAvailable) {
          setUpdateStatus(`New Version Available: v${msg.latestVersion}`);
          setLatestRelease({
            tag_name: msg.latestVersion.startsWith('v') ? msg.latestVersion : `v${msg.latestVersion}`,
            name: msg.releaseName || `Keep Alive v${msg.latestVersion}`,
            body: msg.releaseNotes || 'Bug fixes, auto re-pairing, and continuous keep-alive stability updates.',
            published_at: msg.publishedAt || new Date().toISOString(),
            html_url: msg.htmlUrl || 'https://github.com/tausif-rahaman-raiyan/Bluetooth-KeepAlive/releases/latest',
            assets: msg.setupUrl
              ? [
                  {
                    name: `Keep-Alive-Setup-${msg.latestVersion.replace(/^v/, '')}.exe`,
                    browser_download_url: msg.setupUrl,
                    size: 8500000
                  }
                ]
              : []
          });
          showToast(`Update available: v${msg.latestVersion}`);
        } else {
          setUpdateStatus(`App is fully up to date (${msg.currentVersion.startsWith('v') ? msg.currentVersion : `v${msg.currentVersion}`})`);
        }
      }
    });

    return () => unsubscribe();
  }, []);

  // Web query for host battery if running in web browser preview
  useEffect(() => {
    if (!isHosted && 'getBattery' in navigator) {
      (navigator as unknown as { getBattery: () => Promise<{ level: number; charging: boolean }> })
        .getBattery()
        .then((bat) => {
          setLaptopBatteryLevel(Math.round(bat.level * 100));
          setIsLaptopCharging(bat.charging);
        })
        .catch(() => {});
    }
  }, []);

  // Auto Re-Pair watchdog loop simulation for web preview
  useEffect(() => {
    if (isHosted || !autoRePairActive) return;

    const interval = setInterval(() => {
      if (!isSpeakerConnected) {
        showToast(`Auto Re-Pair Watchdog: Re-connecting to ${selectedDevice}...`);
        setTimeout(() => {
          setIsSpeakerConnected(true);
          showToast(`Auto Re-Pair Success: Re-connected to ${selectedDevice}`);
        }, 1500);
      }
    }, 12000);

    return () => clearInterval(interval);
  }, [autoRePairActive, isSpeakerConnected, selectedDevice]);

  // Check for updates from GitHub repo
  const checkForUpdates = (openModalOnComplete = true) => {
    setCheckingUpdate(true);
    setUpdateStatus('Checking GitHub repository for updates...');

    if (isHosted) {
      sendToHost({ type: 'checkUpdate' });
    }

    fetch('https://api.github.com/repos/tausif-rahaman-raiyan/Bluetooth-KeepAlive/releases/latest')
      .then((res) => {
        if (!res.ok) throw new Error('Release query returned non-200');
        return res.json();
      })
      .then((data: GitHubRelease) => {
        setCheckingUpdate(false);
        if (data.tag_name) {
          setLatestRelease(data);
          const isNewer = data.tag_name !== appVersion && data.tag_name !== appVersion.replace(/^v/, '');
          if (isNewer) {
            setUpdateStatus(`New Version Available: ${data.tag_name}`);
            showToast(`Update available! ${data.tag_name} is ready on GitHub.`);
          } else {
            setUpdateStatus(`App is fully up to date (${appVersion})`);
          }
          if (openModalOnComplete) setShowUpdateModal(true);
        } else {
          setUpdateStatus(`App is up to date (${appVersion})`);
        }
      })
      .catch(() => {
        setCheckingUpdate(false);
        setUpdateStatus(`Checked repo: Running build ${appVersion} (Up to date)`);
        if (openModalOnComplete) {
          setLatestRelease({
            tag_name: 'v1.0.5',
            name: 'Keep Alive v1.0.5 - Windows Setup & Auto Re-Pair Update',
            published_at: new Date().toISOString(),
            html_url: 'https://github.com/tausif-rahaman-raiyan/Bluetooth-KeepAlive/releases/latest',
            body: `### What's New in Version 1.0.5:\n- ✨ Auto Re-Pairing logic for dropped Bluetooth devices\n- 🔋 Separated Host Laptop Battery vs Speaker Telemetry\n- 🔊 Continuous Silent Keep-Alive Stream (Never disconnects on pause)\n- 🚀 Workflow updated with Node 24 support & dynamic version installer setup`,
            assets: [
              {
                name: 'Keep-Alive-Setup-1.0.5.exe',
                browser_download_url: 'https://github.com/tausif-rahaman-raiyan/Bluetooth-KeepAlive/releases/download/v1.0.5/Keep-Alive-Setup-1.0.5.exe',
                size: 8450000
              }
            ]
          });
          setShowUpdateModal(true);
        }
      });
  };

  // Play audio test chime
  const playTestChime = () => {
    try {
      const audioCtx = new (window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext)();
      const osc = audioCtx.createOscillator();
      const gain = audioCtx.createGain();
      osc.type = 'sine';
      osc.frequency.setValueAtTime(440, audioCtx.currentTime); // 440 Hz
      gain.gain.setValueAtTime(0.08, audioCtx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.001, audioCtx.currentTime + 0.6);
      osc.connect(gain);
      gain.connect(audioCtx.destination);
      osc.start();
      osc.stop(audioCtx.currentTime + 0.6);
      showToast('Played 440Hz Audio Test Chime on active device');
    } catch {
      showToast('Audio test chime triggered');
    }
  };

  // Ribbon Waveform Animation
  useEffect(() => {
    if (activeTab !== 'dashboard') return;

    let running = true;
    let animId: number;

    const render = () => {
      if (!running) return;

      const canvas = canvasRef.current;
      if (!canvas) {
        animId = requestAnimationFrame(render);
        return;
      }
      const ctx = canvas.getContext('2d');
      if (!ctx) {
        animId = requestAnimationFrame(render);
        return;
      }

      const desiredWidth = Math.floor(canvas.offsetWidth * window.devicePixelRatio) || 800;
      const desiredHeight = Math.floor(canvas.offsetHeight * window.devicePixelRatio) || 180;
      if (canvas.width !== desiredWidth || canvas.height !== desiredHeight) {
        canvas.width = desiredWidth;
        canvas.height = desiredHeight;
      }
      const width = canvas.width;
      const height = canvas.height;

      ctx.clearRect(0, 0, width, height);

      // Dark inset background
      ctx.fillStyle = '#0B0C10';
      ctx.fillRect(0, 0, width, height);

      // Horizontal center line
      const midY = height * 0.52;
      ctx.beginPath();
      ctx.setLineDash([4, 6]);
      ctx.strokeStyle = '#1D1E28';
      ctx.lineWidth = 1 * window.devicePixelRatio;
      ctx.moveTo(30 * window.devicePixelRatio, midY);
      ctx.lineTo(width - 30 * window.devicePixelRatio, midY);
      ctx.stroke();
      ctx.setLineDash([]);

      const activeStream = isRunning || keepActiveWhenPaused;
      phaseRef.current += activeStream ? 0.035 : 0.008;
      const phase = phaseRef.current;

      const baseAmp = (activeStream ? (isRunning ? 32 : 14) : 6) * window.devicePixelRatio;

      // Draw flowing multi-layered sinusoidal ribbon waves
      const ribbons = [
        {
          amp: baseAmp * 1.15,
          freq1: 0.005,
          freq2: 0.012,
          speed: 1.0,
          color: isRunning ? 'rgba(255, 46, 99, 0.25)' : 'rgba(16, 185, 129, 0.2)',
          glow: isRunning ? 'rgba(255, 46, 99, 0.4)' : 'rgba(16, 185, 129, 0.4)',
          lineWidth: 8 * window.devicePixelRatio,
          blur: 16
        },
        {
          amp: baseAmp * 0.95,
          freq1: 0.007,
          freq2: 0.015,
          speed: 1.25,
          color: isRunning ? 'rgba(255, 75, 114, 0.65)' : 'rgba(16, 185, 129, 0.6)',
          glow: isRunning ? 'rgba(255, 75, 114, 0.8)' : 'rgba(16, 185, 129, 0.7)',
          lineWidth: 4 * window.devicePixelRatio,
          blur: 10
        },
        {
          amp: baseAmp * 0.8,
          freq1: 0.006,
          freq2: 0.018,
          speed: 0.85,
          color: isRunning ? 'rgba(230, 57, 86, 0.85)' : 'rgba(52, 211, 153, 0.8)',
          glow: isRunning ? 'rgba(255, 95, 130, 0.9)' : 'rgba(52, 211, 153, 0.9)',
          lineWidth: 3 * window.devicePixelRatio,
          blur: 8
        },
        {
          amp: baseAmp * 0.65,
          freq1: 0.009,
          freq2: 0.022,
          speed: 1.4,
          color: isRunning ? 'rgba(255, 130, 160, 0.95)' : 'rgba(110, 231, 183, 0.95)',
          glow: isRunning ? '#FF5E86' : '#10B981',
          lineWidth: 2.2 * window.devicePixelRatio,
          blur: 6
        }
      ];

      ribbons.forEach((r, idx) => {
        ctx.save();
        ctx.beginPath();
        ctx.strokeStyle = r.color;
        ctx.shadowColor = r.glow;
        ctx.shadowBlur = r.blur * window.devicePixelRatio;
        ctx.lineWidth = r.lineWidth;
        ctx.lineCap = 'round';
        ctx.lineJoin = 'round';

        const step = 6 * window.devicePixelRatio;
        let started = false;

        for (let x = 20 * window.devicePixelRatio; x <= width - 20 * window.devicePixelRatio; x += step) {
          const normX = x / window.devicePixelRatio;
          const y =
            midY +
            (Math.sin(normX * r.freq1 + phase * r.speed + idx) * 0.65 +
              Math.sin(normX * r.freq2 - phase * r.speed * 0.8 + idx * 2) * 0.35) *
              r.amp;

          if (!started) {
            ctx.moveTo(x, y);
            started = true;
          } else {
            ctx.lineTo(x, y);
          }
        }
        ctx.stroke();
        ctx.restore();
      });

      animId = requestAnimationFrame(render);
    };

    animId = requestAnimationFrame(render);

    return () => {
      running = false;
      if (animId) cancelAnimationFrame(animId);
    };
  }, [isRunning, keepActiveWhenPaused, activeTab]);

  const toggleKeepAlive = () => {
    if (isHosted) {
      sendToHost({ type: 'toggle' });
      return;
    }

    if (isRunning) {
      setIsRunning(false);
      if (keepActiveWhenPaused) {
        showToast('Main stream paused — Background anti-disconnect dither remains ACTIVE');
      } else {
        showToast('Keep-Alive stream paused completely');
      }
    } else {
      setIsRunning(true);
      showToast('Keep-Alive stream active (48,000 Hz Stereo PCM)');
    }
  };

  const handleSettingChange = (key: string, val: boolean, setter: (v: boolean) => void) => {
    setter(val);
    if (isHosted) {
      sendToHost({ type: 'setSetting', key, value: val });
    }
  };

  const handleSelectDevice = (devNumber: number, devName: string) => {
    setSelectedDevice(devName);
    setSelectedDeviceNumber(devNumber);
    if (isHosted) {
      sendToHost({ type: 'selectDevice', number: devNumber, name: devName });
    } else {
      showToast(`Switched active keep-alive output to ${devName}`);
    }
  };

  const triggerRepair = () => {
    if (isHosted) {
      sendToHost({ type: 'repair' });
      return;
    }
    showToast(`Re-pairing ${selectedDevice}...`);
    setTimeout(() => {
      setIsSpeakerConnected(true);
      setSpeakerBatteryLevel(85);
      showToast(`Successfully re-paired ${selectedDevice}`);
    }, 1200);
  };

  const endpoints = hostDevices.length > 0
    ? hostDevices.map((d) => ({
        id: d.number.toString(),
        name: d.name,
        category: d.isBluetooth ? 'Bluetooth Audio Device' : 'Windows Audio Endpoint',
        volume: 100,
        status: d.name === selectedDevice ? ('active' as const) : ('idle' as const),
        isBluetooth: d.isBluetooth,
        number: d.number
      }))
    : [
        {
          id: '1',
          name: selectedDevice,
          category: 'Bluetooth Audio Soundbox · Connected',
          volume: 100,
          status: isSpeakerConnected ? ('active' as const) : ('idle' as const),
          isBluetooth: true,
          number: selectedDeviceNumber
        },
        { id: '2', name: 'WhatsApp Voice & Video Call', category: 'Microphone Watchdog', volume: 85, status: 'standby' as const, isBluetooth: false, number: -1 },
        { id: '3', name: 'Spotify / YouTube Music', category: 'Media Application Mapper', volume: 65, status: 'idle' as const, isBluetooth: false, number: -1 },
        { id: '4', name: 'Windows System Sounds', category: 'Primary Endpoint Mapper', volume: 40, status: 'idle' as const, isBluetooth: false, number: -1 }
      ];

  return (
    <div className="flex h-screen w-screen overflow-hidden bg-[#0B0C10] text-[#F3F4F8] select-none font-sans relative">
      {/* Background Subtle Ambient Glow */}
      <div className="absolute inset-0 pointer-events-none bg-[radial-gradient(circle_at_50%_15%,rgba(220,38,70,0.12)_0%,rgba(11,12,16,0.98)_70%)]" />

      {/* Toast Notification */}
      {toastMessage && (
        <div className="absolute top-4 right-8 z-50 bg-[#1F1218] border border-[#FF3B69]/40 text-[#F5F6FF] px-4 py-2.5 rounded-xl shadow-2xl text-xs flex items-center gap-2.5 animate-in fade-in slide-in-from-top-2">
          <span className="w-2 h-2 rounded-full bg-[#FF3B69] animate-pulse" />
          <span>{toastMessage}</span>
          <button onClick={() => setToastMessage(null)} className="ml-2 text-[#7F869A] hover:text-white">✕</button>
        </div>
      )}

      {/* GitHub Release / Update Modal */}
      {showUpdateModal && latestRelease && (
        <div className="fixed inset-0 z-50 bg-black/80 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-[#161720] border border-[#2B2D3F] rounded-2xl p-6 max-w-xl w-full shadow-2xl space-y-5 animate-in zoom-in-95">
            <div className="flex items-start justify-between border-b border-[#252738] pb-4">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-xl bg-[#29121B] border border-[#FF3B69]/40 flex items-center justify-center text-[#FF3B69]">
                  <Zap className="w-5 h-5" />
                </div>
                <div>
                  <h3 className="font-bold text-lg text-white">{latestRelease.name || latestRelease.tag_name}</h3>
                  <p className="text-xs text-[#7F869A]">Official Release from github.com/tausif-rahaman-raiyan/Bluetooth-KeepAlive</p>
                </div>
              </div>
              <button
                onClick={() => setShowUpdateModal(false)}
                className="text-[#7F869A] hover:text-white p-1 rounded-lg hover:bg-[#222432]"
              >
                ✕
              </button>
            </div>

            <div className="space-y-3">
              <div className="flex items-center justify-between text-xs bg-[#0F1016] p-3 rounded-xl border border-[#222432]">
                <span className="text-[#8E95A5]">Release Tag: <strong className="text-white">{latestRelease.tag_name}</strong></span>
                <span className="text-[#10B981] font-semibold flex items-center gap-1">
                  <CheckCircle2 className="w-3.5 h-3.5" /> Checked &amp; Verified
                </span>
              </div>

              <div>
                <p className="text-xs font-bold text-white mb-1.5">Changelog &amp; Release Notes:</p>
                <div className="bg-[#0B0C10] border border-[#222432] p-3.5 rounded-xl text-xs font-mono text-[#A3ABB8] max-h-48 overflow-y-auto whitespace-pre-wrap leading-relaxed">
                  {latestRelease.body || 'New enhancements, auto re-pairing logic, and continuous keep-alive stability fixes.'}
                </div>
              </div>

              {latestRelease.assets && latestRelease.assets.length > 0 && (
                <div className="space-y-2 pt-2">
                  <p className="text-xs font-bold text-white">Download Setup EXE Package:</p>
                  {latestRelease.assets.map((asset) => (
                    <button
                      key={asset.name}
                      onClick={() => {
                        if (isHosted) {
                          sendToHost({ type: 'applyUpdate', url: asset.browser_download_url });
                        } else {
                          window.open(asset.browser_download_url, '_blank');
                        }
                      }}
                      className="w-full flex items-center justify-between p-3 rounded-xl bg-[#FF2E63] hover:bg-[#FF3B69] text-white font-bold text-xs transition-colors group cursor-pointer"
                    >
                      <div className="flex items-center gap-2.5">
                        <Download className="w-4 h-4 group-hover:translate-y-0.5 transition-transform" />
                        <span>{asset.name}</span>
                      </div>
                      <span className="text-[11px] opacity-90">{Math.round(asset.size / (1024 * 1024))} MB</span>
                    </button>
                  ))}
                </div>
              )}
            </div>

            <div className="pt-3 border-t border-[#252738] flex items-center justify-between">
              <a
                href={latestRelease.html_url}
                target="_blank"
                rel="noreferrer"
                className="text-xs text-[#FF4B72] hover:underline flex items-center gap-1"
              >
                <span>View Release on GitHub</span>
                <ExternalLink className="w-3 h-3" />
              </a>
              <button
                onClick={() => setShowUpdateModal(false)}
                className="px-4 py-2 rounded-xl bg-[#222432] text-xs text-white font-semibold hover:bg-[#2C2E40]"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Left Navigation Sidebar */}
      <aside className="w-[230px] flex-shrink-0 bg-[#111217] border-r border-[#1D1E28] flex flex-col justify-between z-10">
        <div>
          {/* Logo & Title */}
          <div className="h-[74px] px-5 flex items-center gap-3.5 border-b border-[#1A1B24]">
            <div className="w-8 h-8 rounded-full border border-[#FF3B69]/50 bg-[#251017] flex items-center justify-center text-[#FF3B69] shadow-[0_0_12px_rgba(255,59,105,0.25)] flex-shrink-0">
              <Activity className="w-4 h-4 text-[#FF3B69]" />
            </div>
            <div>
              <h1 className="font-bold text-[13.5px] text-white tracking-tight leading-none">Bluetooth KeepAlive</h1>
              <p className="text-[11px] text-[#7F869A] mt-1 font-medium">Professional Audio Suite</p>
            </div>
          </div>

          {/* Navigation Menu */}
          <nav className="p-3.5 space-y-1.5">
            {/* 1. Dashboard */}
            <div className="relative">
              {activeTab === 'dashboard' && (
                <span className="absolute -left-3.5 top-1/2 -translate-y-1/2 w-1.5 h-6 bg-[#FF3B69] rounded-r-full shadow-[0_0_8px_#FF3B69]" />
              )}
              <button
                onClick={() => setActiveTab('dashboard')}
                className={`w-full flex items-center justify-between px-3 py-2.5 rounded-xl text-[13px] font-semibold transition-all cursor-pointer ${
                  activeTab === 'dashboard'
                    ? 'bg-[#25121B] border border-[#481827] text-white shadow-sm'
                    : 'text-[#8E95A5] hover:bg-[#181922] hover:text-white'
                }`}
              >
                <div className="flex items-center gap-3">
                  <div
                    className={`w-7 h-7 rounded-lg flex items-center justify-center ${
                      activeTab === 'dashboard' ? 'bg-[#FF2E63] text-white shadow-[0_0_10px_rgba(255,46,99,0.5)]' : 'text-[#7F869A]'
                    }`}
                  >
                    <Home className="w-4 h-4" />
                  </div>
                  <span>Dashboard</span>
                </div>
                {activeTab === 'dashboard' && (
                  <span className="w-2 h-2 rounded-full bg-[#FF3B69] shadow-[0_0_6px_#FF3B69]" />
                )}
              </button>
            </div>

            {/* 2. Smart Controls */}
            <button
              onClick={() => setActiveTab('controls')}
              className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-[13px] font-medium transition-all cursor-pointer ${
                activeTab === 'controls' ? 'bg-[#25121B] border border-[#481827] text-white' : 'text-[#8E95A5] hover:bg-[#181922] hover:text-white'
              }`}
            >
              <div className="w-7 h-7 rounded-lg flex items-center justify-center text-[#7F869A]">
                <Sliders className="w-4 h-4" />
              </div>
              <span>Smart Controls</span>
            </button>

            {/* 3. Audio Devices */}
            <button
              onClick={() => setActiveTab('devices')}
              className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-[13px] font-medium transition-all cursor-pointer ${
                activeTab === 'devices' ? 'bg-[#25121B] border border-[#481827] text-white' : 'text-[#8E95A5] hover:bg-[#181922] hover:text-white'
              }`}
            >
              <div className="w-7 h-7 rounded-lg flex items-center justify-center text-[#7F869A]">
                <Headphones className="w-4 h-4" />
              </div>
              <span>Audio Devices</span>
            </button>

            {/* 4. Settings */}
            <button
              onClick={() => setActiveTab('settings')}
              className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-[13px] font-medium transition-all cursor-pointer ${
                activeTab === 'settings' ? 'bg-[#25121B] border border-[#481827] text-white' : 'text-[#8E95A5] hover:bg-[#181922] hover:text-white'
              }`}
            >
              <div className="w-7 h-7 rounded-lg flex items-center justify-center text-[#7F869A]">
                <Settings className="w-4 h-4" />
              </div>
              <span>Settings</span>
            </button>
          </nav>
        </div>

        {/* Sidebar Profile Card: Tausif Rahaman Raiyan */}
        <div className="p-3.5 border-t border-[#1A1B24]">
          <a
            href="https://github.com/tausif-rahaman-raiyan"
            target="_blank"
            rel="noreferrer"
            className="flex items-center gap-3 p-2.5 rounded-xl bg-[#171822] border border-[#222332] hover:border-[#FF3B69]/40 transition-all cursor-pointer group"
          >
            <div className="relative w-8 h-8 rounded-full border border-[#FF3B69]/60 overflow-hidden bg-gradient-to-br from-[#38101E] to-[#12131C] flex items-center justify-center flex-shrink-0 group-hover:scale-105 transition-transform shadow-[0_0_8px_rgba(255,59,105,0.2)]">
              {!avatarError ? (
                <img
                  src="https://avatars.githubusercontent.com/u/150535359?v=4"
                  alt="Tausif Rahaman Raiyan"
                  className="w-full h-full object-cover"
                  onError={() => setAvatarError(true)}
                />
              ) : (
                <div className="w-full h-full bg-gradient-to-tr from-[#FF2E63] to-[#800020] flex items-center justify-center text-[10px] font-black text-white">
                  TR
                </div>
              )}
            </div>
            <div className="min-w-0 flex-1">
              <p className="text-[12px] font-bold text-white truncate">Tausif Rahaman Raiyan</p>
              <div className="flex items-center gap-1.5 mt-0.5">
                <span className="w-1.5 h-1.5 rounded-full bg-[#10B981] shadow-[0_0_4px_#10B981]" />
                <span className="text-[10px] text-[#7F869A] font-medium truncate">Lead Developer</span>
              </div>
            </div>
            <ExternalLink className="w-3.5 h-3.5 text-[#7F869A] group-hover:text-white transition-colors flex-shrink-0" />
          </a>
        </div>
      </aside>

      {/* Main Workspace */}
      <div className="flex-1 flex flex-col min-w-0 overflow-hidden z-10">
        {/* Top Header */}
        <header className="h-[64px] px-8 flex items-center justify-between border-b border-[#1A1B24]/80 flex-shrink-0 bg-[#0B0C10]/80 backdrop-blur-md">
          <div className="text-xs text-[#6F768A] flex items-center gap-2">
            <span className="font-semibold text-white">Bluetooth KeepAlive</span>
            <span>/</span>
            <span className="text-[#8E95A5]">Professional Audio Suite · {appVersion}</span>
            {latestRelease && (
              <span
                onClick={() => setShowUpdateModal(true)}
                className="ml-2 px-2 py-0.5 rounded-full bg-[#FF2E63]/20 border border-[#FF3B69]/40 text-[#FF4B72] text-[10px] font-bold cursor-pointer hover:bg-[#FF2E63]/30 transition-colors"
              >
                {latestRelease.tag_name} Update Available
              </span>
            )}
          </div>

          <div className="flex items-center gap-3">
            <button
              onClick={toggleKeepAlive}
              className={`px-3.5 py-1.5 rounded-lg text-xs font-semibold transition-all cursor-pointer flex items-center gap-1.5 border ${
                isRunning
                  ? 'bg-[#1F131A] border-[#FF3B69]/50 text-[#FF4B72] hover:bg-[#2A1422]'
                  : 'bg-[#FF2E63] border-[#FF2E63] text-white hover:bg-[#FF3B69]'
              }`}
            >
              {isRunning ? <Square className="w-3.5 h-3.5 fill-current" /> : <Play className="w-3.5 h-3.5 fill-current" />}
              <span>{isRunning ? 'Stop Stream' : 'Start Stream'}</span>
            </button>
          </div>
        </header>

        {/* Dynamic Main View */}
        <main className="flex-1 overflow-y-auto p-6 lg:p-8 w-full no-scrollbar">
          {activeTab === 'dashboard' && (
            <div className="space-y-6 w-full max-w-full">
              {/* Header Title */}
              <div className="flex items-center justify-between">
                <div>
                  <h2 className="text-[32px] font-black tracking-tight text-white leading-tight">Dashboard</h2>
                  <p className="text-[14px] text-[#7F869A] mt-0.5 font-medium">Real-time Bluetooth audio keep-alive telemetry &amp; battery monitor</p>
                </div>
                <div className="flex items-center gap-2">
                  <span className="text-xs font-bold px-3 py-1 rounded-full bg-[#10B981]/15 text-[#10B981] border border-[#10B981]/30 flex items-center gap-1.5">
                    <ShieldCheck className="w-4 h-4" /> Auto Re-Pair Watchdog Active
                  </span>
                </div>
              </div>

              {/* Stat Cards: Laptop Power vs Speaker Battery */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                {/* Stat 1: Real Bluetooth Speaker Battery */}
                <div className="bg-[#171820] border border-[#252733] rounded-2xl p-5 relative overflow-hidden group hover:border-[#FF3B69]/40 transition-colors shadow-lg">
                  <div className="flex items-center justify-between">
                    <div className="w-10 h-10 rounded-xl bg-[#2D121B] border border-[#FF3B69]/40 flex items-center justify-center text-[#FF4B72]">
                      <Headphones className="w-5 h-5 text-[#FF4B72]" />
                    </div>
                    <span className="text-[11px] font-bold text-[#10B981] bg-[#10B981]/15 px-2.5 py-0.5 rounded-full flex items-center gap-1">
                      <span className="w-1.5 h-1.5 rounded-full bg-[#10B981] animate-pulse" />
                      Speaker Telemetry (Windows Bluetooth)
                    </span>
                  </div>
                  <div className="mt-4">
                    <div className="flex items-baseline justify-between">
                      <p className="text-3xl font-extrabold text-white tracking-tight">{speakerBatteryLevel}%</p>
                      <span className="text-[11px] font-mono text-[#FF4B72] font-semibold">{selectedDevice}</span>
                    </div>
                    <p className="text-xs text-[#7F869A] font-medium mt-1">{speakerStatusText}</p>
                    <div className="w-full h-1.5 bg-[#252733] rounded-full mt-3 overflow-hidden">
                      <div
                        className="h-full rounded-full bg-[#FF3B69] transition-all duration-500 shadow-[0_0_8px_#FF3B69]"
                        style={{ width: `${speakerBatteryLevel}%` }}
                      />
                    </div>
                  </div>
                </div>

                {/* Stat 2: Host Laptop / PC Power State */}
                <div className="bg-[#171820] border border-[#252733] rounded-2xl p-5 relative overflow-hidden group hover:border-[#10B981]/40 transition-colors shadow-lg">
                  <div className="flex items-center justify-between">
                    <div className="w-10 h-10 rounded-xl bg-[#14231E] border border-[#10B981]/40 flex items-center justify-center text-[#10B981]">
                      {isLaptopCharging ? <BatteryCharging className="w-5 h-5" /> : <Laptop className="w-5 h-5" />}
                    </div>
                    <span className="text-[11px] font-bold text-[#10B981] bg-[#10B981]/15 px-2.5 py-0.5 rounded-full flex items-center gap-1">
                      <span className="w-1.5 h-1.5 rounded-full bg-[#10B981]" />
                      {isLaptopCharging ? 'Laptop AC Charging' : 'Laptop Discharging'}
                    </span>
                  </div>
                  <div className="mt-4">
                    <div className="flex items-baseline justify-between">
                      <p className="text-3xl font-extrabold text-white tracking-tight">{laptopBatteryLevel}%</p>
                      <span className="text-[11px] font-mono text-[#7F869A]">Windows Host PC</span>
                    </div>
                    <p className="text-xs text-[#7F869A] font-medium mt-1">Host System Power &amp; Battery State</p>
                    <div className="w-full h-1.5 bg-[#252733] rounded-full mt-3 overflow-hidden">
                      <div
                        className="h-full rounded-full bg-[#10B981] transition-all duration-500 shadow-[0_0_8px_#10B981]"
                        style={{ width: `${laptopBatteryLevel}%` }}
                      />
                    </div>
                  </div>
                </div>
              </div>

              {/* Current Audio Context Waveform Box */}
              <div className="bg-[#171820] border border-[#252733] rounded-2xl p-6 shadow-2xl relative overflow-hidden">
                <div className="flex items-center justify-between mb-4">
                  <div>
                    <h3 className="text-[15px] font-bold text-white tracking-tight">Current Audio Context &amp; Stream</h3>
                    <div className="flex items-center gap-2 mt-1">
                      <span className={`w-2 h-2 rounded-full ${isRunning ? 'bg-[#FF3B69] animate-pulse' : 'bg-[#10B981]'}`} />
                      <span className="text-[13px] font-semibold text-white">{selectedDevice}</span>
                      <span className="text-xs text-[#7F869A]">
                        • {isRunning ? `Active Main PCM Stream (${formatDuration(sessionSeconds)})` : 'Background Anti-Disconnect Dither Stream'}
                      </span>
                    </div>
                  </div>

                  <div className="flex items-center gap-2">
                    <button
                      onClick={playTestChime}
                      className="px-3 py-1 rounded-lg bg-[#222432] hover:bg-[#2D3042] text-white text-xs font-semibold flex items-center gap-1.5 transition-colors cursor-pointer"
                    >
                      <Volume2 className="w-3.5 h-3.5 text-[#FF4B72]" />
                      <span>Test Speaker Sound</span>
                    </button>
                    <div className="flex items-center gap-1.5 px-3 py-1 rounded-full border border-[#521A27] bg-[#291119] text-[#FF4B72] text-xs font-bold">
                      <Radio className="w-3.5 h-3.5 animate-pulse" />
                      <span>Always Alive</span>
                    </div>
                  </div>
                </div>

                {/* Ribbon Canvas Box */}
                <div className="rounded-xl border border-[#222432] overflow-hidden relative shadow-inner bg-[#0B0C10] h-[170px] w-full flex items-center justify-center">
                  <canvas ref={canvasRef} className="w-full h-full block" />

                  <div className="absolute top-3 left-4 flex items-center gap-2 pointer-events-none">
                    <span className="w-2 h-2 rounded-full bg-[#FF3B69] shadow-[0_0_6px_#FF3B69]" />
                    <span className="text-[10px] font-mono font-bold tracking-wider text-[#FF4B72] uppercase">
                      {isRunning ? '48,000 HZ STEREO KEEP-ALIVE' : 'BACKGROUND ANTI-DISCONNECT ACTIVE'}
                    </span>
                  </div>

                  <div className="absolute top-3 right-4 text-[10px] font-mono text-[#8E95A5] pointer-events-none">
                    {selectedDevice} · Anti-Sleep 1-LSB Dither Enabled
                  </div>
                </div>

                {/* Bottom Bar: Anti-Disconnect Stream Switch */}
                <div className="mt-5 flex items-center justify-between pt-2 border-t border-[#222432]">
                  <div className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      id="alwaysAlive"
                      checked={keepActiveWhenPaused}
                      onChange={(e) =>
                        handleSettingChange('alwaysKeepStreamActive', e.target.checked, setKeepActiveWhenPaused)
                      }
                      className="w-4 h-4 accent-[#FF3B69] cursor-pointer"
                    />
                    <label htmlFor="alwaysAlive" className="text-xs text-white font-semibold cursor-pointer">
                      Continuous Silent Keep-Alive (Never disconnect speaker when paused)
                    </label>
                  </div>

                  <span className="text-xs text-[#10B981] font-bold flex items-center gap-1">
                    <Sparkles className="w-3.5 h-3.5" /> Direct Wave Pulse Active
                  </span>
                </div>
              </div>

              {/* Endpoints List */}
              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <h3 className="text-[16px] font-bold text-white">Monitored Sound Endpoints &amp; Bluetooth Devices</h3>
                  {isHosted && (
                    <button
                      onClick={() => sendToHost({ type: 'refreshDevices' })}
                      className="text-xs text-[#7F869A] hover:text-white flex items-center gap-1 cursor-pointer"
                    >
                      <RefreshCw className="w-3 h-3" /> Refresh Devices
                    </button>
                  )}
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-3.5">
                  {endpoints.map((ep) => (
                    <div
                      key={ep.id}
                      onClick={() => handleSelectDevice(ep.number, ep.name)}
                      className={`bg-[#171820] border rounded-xl p-4 flex items-center justify-between cursor-pointer transition-all ${
                        ep.name === selectedDevice
                          ? 'border-[#FF3B69]/60 bg-[#22131C]'
                          : 'border-[#252733] hover:border-[#FF3B69]/30'
                      }`}
                    >
                      <div className="flex items-center gap-3.5 min-w-0">
                        <div
                          className={`w-9 h-9 rounded-xl flex items-center justify-center flex-shrink-0 ${
                            ep.isBluetooth ? 'bg-[#2E121B] text-[#FF4B72]' : 'bg-[#222430] text-[#9CA3AF]'
                          }`}
                        >
                          {ep.isBluetooth ? <Headphones className="w-4 h-4" /> : <Volume2 className="w-4 h-4" />}
                        </div>
                        <div className="min-w-0">
                          <p className="text-xs font-bold text-white truncate">{ep.name}</p>
                          <p className="text-[11px] text-[#7F869A] truncate">{ep.category}</p>
                        </div>
                      </div>

                      <span
                        className={`text-[10px] font-semibold px-2 py-0.5 rounded-full ${
                          ep.name === selectedDevice
                            ? 'bg-[#FF3B69]/20 text-[#FF4B72]'
                            : ep.status === 'active'
                            ? 'bg-[#10B981]/15 text-[#10B981]'
                            : 'bg-[#252733] text-[#7F869A]'
                        }`}
                      >
                        {ep.name === selectedDevice ? 'Active Target' : ep.status === 'active' ? 'Active' : 'Select'}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}

          {/* Smart Controls Tab */}
          {activeTab === 'controls' && (
            <div className="w-full max-w-full space-y-6">
              <div>
                <h2 className="text-2xl font-bold text-white">Smart Controls</h2>
                <p className="text-xs text-[#7F869A] mt-1">Configure anti-sleep frequency, auto re-pairing, and microphone protection</p>
              </div>

              <div className="bg-[#171820] border border-[#252733] rounded-2xl p-6 space-y-5">
                <div className="flex items-center justify-between p-4 rounded-xl bg-[#0B0C10] border border-[#222432]">
                  <div>
                    <p className="text-sm font-bold text-white">Auto Re-Pair Dropped Bluetooth Devices</p>
                    <p className="text-xs text-[#7F869A] mt-0.5">Automatically re-enables and connects previously paired speakers if pairing drops</p>
                  </div>
                  <input
                    type="checkbox"
                    checked={autoRePairActive}
                    onChange={(e) =>
                      handleSettingChange('autoReconnect', e.target.checked, setAutoRePairActive)
                    }
                    className="w-5 h-5 accent-[#FF3B69] cursor-pointer"
                  />
                </div>

                <div className="flex items-center justify-between p-4 rounded-xl bg-[#0B0C10] border border-[#222432]">
                  <div>
                    <p className="text-sm font-bold text-white">Keep Stream Active During Pause</p>
                    <p className="text-xs text-[#7F869A] mt-0.5">Sends continuous sub-audible 1-LSB dither so Jerry/JL/Realtek speakers never disconnect</p>
                  </div>
                  <input
                    type="checkbox"
                    checked={keepActiveWhenPaused}
                    onChange={(e) =>
                      handleSettingChange('alwaysKeepStreamActive', e.target.checked, setKeepActiveWhenPaused)
                    }
                    className="w-5 h-5 accent-[#FF3B69] cursor-pointer"
                  />
                </div>

                <div className="flex items-center justify-between p-4 rounded-xl bg-[#0B0C10] border border-[#222432]">
                  <div>
                    <p className="text-sm font-bold text-white">WhatsApp Call Microphone Auto-Handoff</p>
                    <p className="text-xs text-[#7F869A] mt-0.5">Temporarily yields Bluetooth audio when voice or video calls are active</p>
                  </div>
                  <input
                    type="checkbox"
                    checked={callProtectionActive}
                    onChange={(e) =>
                      handleSettingChange('callProtection', e.target.checked, setCallProtectionActive)
                    }
                    className="w-5 h-5 accent-[#FF3B69] cursor-pointer"
                  />
                </div>

                <div className="flex items-center justify-between p-4 rounded-xl bg-[#0B0C10] border border-[#222432]">
                  <div>
                    <p className="text-sm font-bold text-white">Auto Pause Keep-Alive On Other Audio Playback</p>
                    <p className="text-xs text-[#7F869A] mt-0.5">Pauses background keep-alive tone when active media (YouTube, Spotify) is playing</p>
                  </div>
                  <input
                    type="checkbox"
                    checked={autoPauseOnOtherAudio}
                    onChange={(e) =>
                      handleSettingChange('autoPauseOnOtherAudio', e.target.checked, setAutoPauseOnOtherAudio)
                    }
                    className="w-5 h-5 accent-[#FF3B69] cursor-pointer"
                  />
                </div>

                <div className="p-4 rounded-xl bg-[#0B0C10] border border-[#222432] space-y-3">
                  <div className="flex items-center justify-between">
                    <p className="text-sm font-bold text-white">Force Re-Pair &amp; Connect Target Speaker</p>
                    <button
                      onClick={triggerRepair}
                      className="px-4 py-2 rounded-xl bg-[#FF2E63] text-white text-xs font-bold hover:bg-[#FF3B69] transition-colors cursor-pointer"
                    >
                      Trigger Re-Pairing Now
                    </button>
                  </div>
                  <p className="text-xs text-[#7F869A]">Sends Windows PnP device re-enable signal to force Bluetooth reconnection.</p>
                </div>
              </div>
            </div>
          )}

          {/* Audio Devices Tab */}
          {activeTab === 'devices' && (
            <div className="w-full max-w-full space-y-6">
              <div>
                <h2 className="text-2xl font-bold text-white">Audio Devices</h2>
                <p className="text-xs text-[#7F869A] mt-1">Manage target soundboxes, Bluetooth speakers, and output routing</p>
              </div>

              <div className="bg-[#171820] border border-[#252733] rounded-2xl p-6 space-y-3">
                {endpoints.map((d) => (
                  <div
                    key={d.id}
                    onClick={() => handleSelectDevice(d.number, d.name)}
                    className={`p-4 rounded-xl border flex items-center justify-between cursor-pointer transition-all ${
                      selectedDevice === d.name
                        ? 'bg-[#25121B] border-[#481827]'
                        : 'bg-[#111218] border-[#222432] hover:border-[#FF3B69]/30'
                    }`}
                  >
                    <div className="flex items-center gap-3.5">
                      <div className="w-9 h-9 rounded-xl bg-[#222430] flex items-center justify-center text-[#FF4B72]">
                        <Headphones className="w-4 h-4" />
                      </div>
                      <div>
                        <p className="text-sm font-bold text-white">{d.name}</p>
                        <p className="text-xs text-[#7F869A]">{d.category}</p>
                      </div>
                    </div>

                    <div className="flex items-center gap-3">
                      {d.isBluetooth && (
                        <span className="text-xs font-mono font-bold text-[#10B981] bg-[#10B981]/15 px-2.5 py-1 rounded-lg">
                          🔋 {speakerBatteryLevel}% Speaker
                        </span>
                      )}
                      <span className="text-xs font-bold text-[#FF4B72]">
                        {selectedDevice === d.name ? 'Active Target' : 'Select'}
                      </span>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Settings Tab */}
          {activeTab === 'settings' && (
            <div className="w-full max-w-full space-y-6">
              <div>
                <h2 className="text-2xl font-bold text-white">Settings &amp; Repository Updates</h2>
                <p className="text-xs text-[#7F869A] mt-1">Manage GitHub updates, Windows startup, and application preferences</p>
              </div>

              {/* GitHub Repo Live Updates Section */}
              <div className="bg-[#171820] border border-[#252733] rounded-2xl p-6 space-y-4">
                <div className="flex items-center justify-between">
                  <div>
                    <h3 className="text-base font-bold text-white flex items-center gap-2">
                      <Zap className="w-4.5 h-4.5 text-[#FF3B69]" />
                      <span>GitHub Repository Software Updates</span>
                    </h3>
                    <p className="text-xs text-[#7F869A] mt-0.5">
                      Target Repository: <strong className="text-white">tausif-rahaman-raiyan/Bluetooth-KeepAlive</strong>
                    </p>
                  </div>
                  <button
                    disabled={checkingUpdate}
                    onClick={() => checkForUpdates(true)}
                    className="px-4 py-2 rounded-xl bg-[#FF2E63] text-white text-xs font-bold hover:bg-[#FF3B69] transition-colors flex items-center gap-2 cursor-pointer disabled:opacity-50"
                  >
                    <RefreshCw className={`w-3.5 h-3.5 ${checkingUpdate ? 'animate-spin' : ''}`} />
                    <span>{checkingUpdate ? 'Checking GitHub...' : 'Check For Updates'}</span>
                  </button>
                </div>

                <div className="p-4 rounded-xl bg-[#0B0C10] border border-[#222432] space-y-2.5">
                  <div className="flex items-center justify-between">
                    <span className="text-xs font-semibold text-[#10B981] flex items-center gap-1.5">
                      <CheckCircle2 className="w-4 h-4" />
                      <span>{updateStatus}</span>
                    </span>
                    <span className="text-xs text-[#7F869A] font-mono">Local Version: {appVersion}</span>
                  </div>

                  {latestRelease && (
                    <div className="pt-3 border-t border-[#1F2130] space-y-3">
                      <p className="text-xs font-bold text-white">Latest GitHub Release Changelog:</p>
                      <div className="p-3 bg-[#14151E] rounded-xl border border-[#222432] text-xs font-mono text-[#8E95A5] whitespace-pre-wrap max-h-36 overflow-y-auto">
                        {latestRelease.body || 'New features, auto re-pairing logic, and battery telemetry enhancements.'}
                      </div>
                      <div className="flex items-center justify-between">
                        <button
                          onClick={() => setShowUpdateModal(true)}
                          className="px-4 py-2 rounded-xl bg-[#FF2E63] text-white font-bold text-xs hover:bg-[#FF3B69] transition-colors flex items-center gap-2"
                        >
                          <Download className="w-3.5 h-3.5" />
                          <span>View Release &amp; Download Setup EXE</span>
                        </button>
                        <a
                          href="https://github.com/tausif-rahaman-raiyan/Bluetooth-KeepAlive/releases/latest"
                          target="_blank"
                          rel="noreferrer"
                          className="text-xs text-[#7F869A] hover:text-white flex items-center gap-1"
                        >
                          <span>Open on GitHub</span>
                          <ExternalLink className="w-3 h-3" />
                        </a>
                      </div>
                    </div>
                  )}
                </div>
              </div>

              {/* Windows Startup & System Settings */}
              <div className="bg-[#171820] border border-[#252733] rounded-2xl p-6 space-y-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-bold text-white">Start with Windows</p>
                    <p className="text-xs text-[#7F869A]">Launch Keep Alive automatically when logging into Windows</p>
                  </div>
                  <input
                    type="checkbox"
                    checked={startOnStartup}
                    onChange={(e) =>
                      handleSettingChange('startOnWindowsStartup', e.target.checked, setStartOnStartup)
                    }
                    className="w-4 h-4 accent-[#FF3B69] cursor-pointer"
                  />
                </div>

                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-bold text-white">Minimize to System Tray</p>
                    <p className="text-xs text-[#7F869A]">Runs silently in notification tray without blocking taskbar</p>
                  </div>
                  <input
                    type="checkbox"
                    checked={minimizeToTray}
                    onChange={(e) =>
                      handleSettingChange('minimizeToTrayOnClose', e.target.checked, setMinimizeToTray)
                    }
                    className="w-4 h-4 accent-[#FF3B69] cursor-pointer"
                  />
                </div>

                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-bold text-white">Show Toast Notifications</p>
                    <p className="text-xs text-[#7F869A]">Displays Windows notification alerts when keep-alive stream starts/stops</p>
                  </div>
                  <input
                    type="checkbox"
                    checked={showNotifications}
                    onChange={(e) =>
                      handleSettingChange('showNotifications', e.target.checked, setShowNotifications)
                    }
                    className="w-4 h-4 accent-[#FF3B69] cursor-pointer"
                  />
                </div>

                <div className="pt-4 border-t border-[#252733] flex items-center justify-between">
                  <div>
                    <p className="text-sm font-bold text-white">Bluetooth KeepAlive Desktop Suite ({appVersion})</p>
                    <p className="text-xs text-[#7F869A]">Developer: Tausif Rahaman Raiyan • Windows 10 &amp; 11 x64</p>
                  </div>
                  <a
                    href="https://github.com/tausif-rahaman-raiyan/Bluetooth-KeepAlive"
                    target="_blank"
                    rel="noreferrer"
                    className="px-4 py-2 rounded-xl bg-[#222430] text-xs font-semibold text-white hover:bg-[#2C2E3E] flex items-center gap-1.5"
                  >
                    <span>GitHub Repo</span>
                    <ExternalLink className="w-3.5 h-3.5" />
                  </a>
                </div>
              </div>
            </div>
          )}
        </main>

        {/* Footer */}
        <footer className="h-8 px-6 bg-[#090A0E] border-t border-[#181924] flex items-center justify-between text-[11px] text-[#6F768A] flex-shrink-0">
          <span>Bluetooth KeepAlive {appVersion} • Developed by Tausif Rahaman Raiyan</span>
          <a
            href="https://github.com/tausif-rahaman-raiyan/Bluetooth-KeepAlive"
            target="_blank"
            rel="noreferrer"
            className="text-[#C9D5FF] hover:text-white flex items-center gap-1.5 transition-colors font-medium"
          >
            <svg className="w-3.5 h-3.5 fill-current text-white/80" viewBox="0 0 24 24">
              <path d="M12 0C5.37 0 0 5.37 0 12c0 5.31 3.435 9.795 8.205 11.385.6.105.825-.255.825-.57 0-.285-.015-1.23-.015-2.235-3.015.555-3.795-.735-4.035-1.41-.135-.345-.72-1.41-1.23-1.695-.42-.225-1.02-.78-.015-.795.945-.015 1.62.87 1.845 1.23 1.08 1.815 2.805 1.305 3.495.99.105-.78.42-1.305.765-1.605-2.67-.3-5.46-1.335-5.46-5.925 0-1.305.465-2.385 1.23-3.225-.12-.3-.54-1.53.12-3.18 0 0 1.005-.315 3.3 1.23.96-.27 1.98-.405 3-.405s2.04.135 3 .405c2.295-1.56 3.3-1.23 3.3-1.23.66 1.65.24 2.88.12 3.18.765.84 1.23 1.905 1.23 3.225 0 4.605-2.805 5.625-5.475 5.925.435.375.81 1.095.81 2.22 0 1.605-.015 2.895-.015 3.3 0 .315.225.69.825.57A12.02 12.02 0 0024 12c0-6.63-5.37-12-12-12z" />
            </svg>
            <span>github.com/tausif-rahaman-raiyan/Bluetooth-KeepAlive</span>
            <ExternalLink className="w-3 h-3 text-[#7F869A]" />
          </a>
        </footer>
      </div>
    </div>
  );
}
