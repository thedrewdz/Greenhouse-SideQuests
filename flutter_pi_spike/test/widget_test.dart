import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:flutter_pi_spike/main.dart';

void main() {
  testWidgets('renders the spike screen and counts taps', (tester) async {
    await tester.pumpWidget(const FlutterPiSpikeApp());

    expect(find.text('Greenhouse flutter-pi spike'), findsOneWidget);
    expect(find.text('Taps: 0'), findsOneWidget);

    await tester.tapAt(const Offset(100, 100));
    await tester.pump();

    expect(find.text('Taps: 1'), findsOneWidget);
  });
}
