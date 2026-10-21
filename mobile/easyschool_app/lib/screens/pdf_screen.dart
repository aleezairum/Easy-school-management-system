import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:printing/printing.dart';
import 'package:provider/provider.dart';

import '../core/session.dart';
import '../ui/widgets.dart';

/// A school document (voucher, ledger, result card, payslip) - view, share, print.
/// From the server ([path]) or already on the phone ([data], e.g. a chat PDF).
class PdfScreen extends StatelessWidget {
  const PdfScreen({super.key, required this.title, required this.fileName, this.path, this.data});
  final String title, fileName;
  final String? path;
  final Uint8List? data;

  static void open(BuildContext context, {required String title, required String path, required String fileName}) =>
      Navigator.of(context).push(MaterialPageRoute(builder: (_) => PdfScreen(title: title, path: path, fileName: fileName)));

  static void openBytes(BuildContext context, {required String title, required Uint8List data, required String fileName}) =>
      Navigator.of(context).push(MaterialPageRoute(builder: (_) => PdfScreen(title: title, data: data, fileName: fileName)));

  @override
  Widget build(BuildContext context) {
    final api = context.read<Session>().api;
    return Scaffold(
      appBar: AppBar(title: Text(title)),
      body: AsyncView<Uint8List>(
        scrollable: false,
        load: () => data != null ? Future.value(data!) : api.bytes(path!),
        builder: (context, bytes, _) => PdfPreview(
          build: (_) async => bytes,
          pdfFileName: fileName,
          canChangeOrientation: false,
          canChangePageFormat: false,
          canDebug: false,
          allowPrinting: true,
          allowSharing: true,
          maxPageWidth: 900,
          scrollViewDecoration: BoxDecoration(color: Theme.of(context).colorScheme.surfaceContainer),
        ),
      ),
    );
  }
}
