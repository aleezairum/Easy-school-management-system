import 'package:intl/intl.dart';

final _money = NumberFormat('#,##0', 'en_US');

/// Rs 12,500
String rs(num v) => 'Rs ${_money.format(v.round())}';

/// 12,500
String n0(num v) => _money.format(v.round());

/// 51.4%
String pct(num? v) => v == null ? '-' : '${v.toStringAsFixed(v == v.roundToDouble() ? 0 : 1)}%';

/// 14 Oct 2026
String d(DateTime v) => DateFormat('d MMM yyyy').format(v);

/// Wed, 14 Oct
String dayShort(DateTime v) => DateFormat('EEE, d MMM').format(v);

/// October 2026
String monthYear(DateTime v) => DateFormat('MMMM yyyy').format(v);

/// Oct 2026
String monthShort(DateTime v) => DateFormat('MMM yyyy').format(v);

String monthName(int month) => DateFormat('MMMM').format(DateTime(2000, month));

String greeting() {
  final h = DateTime.now().hour;
  return h < 12 ? 'Good morning' : (h < 17 ? 'Good afternoon' : 'Good evening');
}

/// "AB" from "Ali Bhatti".
String initials(String name) {
  final parts = name.trim().split(RegExp(r'\s+')).where((p) => p.isNotEmpty).toList();
  if (parts.isEmpty) return '?';
  return (parts.first[0] + (parts.length > 1 ? parts.last[0] : '')).toUpperCase();
}

/// "Ali" from "Ali Bhatti".
String firstName(String name) => name.trim().split(RegExp(r'\s+')).first;

String ymd(DateTime v) => DateFormat('yyyy-MM-dd').format(v);
