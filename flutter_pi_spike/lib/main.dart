import 'package:flutter/material.dart';

void main() => runApp(const FlutterPiSpikeApp());

/// Minimal app whose only job is to prove a flutter-pi deployment works:
/// it renders full-screen and it responds to touch input.
class FlutterPiSpikeApp extends StatelessWidget {
  const FlutterPiSpikeApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'flutter-pi spike',
      debugShowCheckedModeBanner: false,
      theme: ThemeData.dark(useMaterial3: true),
      home: const SpikeHome(),
    );
  }
}

class SpikeHome extends StatefulWidget {
  const SpikeHome({super.key});

  @override
  State<SpikeHome> createState() => _SpikeHomeState();
}

class _SpikeHomeState extends State<SpikeHome> {
  int _taps = 0;
  Offset _lastTap = Offset.zero;

  void _registerTap(TapDownDetails details) {
    setState(() {
      _taps++;
      _lastTap = details.globalPosition;
    });
  }

  @override
  Widget build(BuildContext context) {
    final size = MediaQuery.sizeOf(context);
    // Flip the background on every tap so touch is unmistakably visible on the
    // physical screen, even from across the room.
    final background =
        _taps.isEven ? Colors.teal.shade900 : Colors.indigo.shade900;

    return Scaffold(
      body: GestureDetector(
        behavior: HitTestBehavior.opaque,
        onTapDown: _registerTap,
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 150),
          width: double.infinity,
          height: double.infinity,
          color: background,
          child: Center(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Icon(Icons.eco, size: 96, color: Colors.greenAccent),
                const SizedBox(height: 24),
                const Text(
                  'Greenhouse flutter-pi spike',
                  style: TextStyle(fontSize: 32, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 12),
                const Text(
                  'It renders. Tap anywhere to prove touch input.',
                  style: TextStyle(fontSize: 18, color: Colors.white70),
                ),
                const SizedBox(height: 32),
                Text('Taps: $_taps', style: const TextStyle(fontSize: 24)),
                const SizedBox(height: 8),
                Text(
                  'Last tap: '
                  '(${_lastTap.dx.toStringAsFixed(0)}, '
                  '${_lastTap.dy.toStringAsFixed(0)})',
                  style: const TextStyle(fontSize: 16, color: Colors.white54),
                ),
                const SizedBox(height: 8),
                Text(
                  'Screen: ${size.width.toStringAsFixed(0)}'
                  ' x ${size.height.toStringAsFixed(0)}',
                  style: const TextStyle(fontSize: 16, color: Colors.white54),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
