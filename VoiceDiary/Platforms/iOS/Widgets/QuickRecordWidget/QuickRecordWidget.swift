//
//  QuickRecordWidget.swift
//  VoiceDiary
//
//  Created on 2026-05-03.
//

import WidgetKit
import SwiftUI

struct Provider: TimelineProvider {
    func placeholder(in context: Context) -> SimpleEntry {
        SimpleEntry(date: Date(), isRecording: false)
    }

    func getSnapshot(in context: Context, completion: @escaping (SimpleEntry) -> ()) {
        let entry = SimpleEntry(date: Date(), isRecording: false)
        completion(entry)
    }

    func getTimeline(in context: Context, completion: @escaping (Timeline<Entry>) -> ()) {
        var entries: [SimpleEntry] = []
        let currentDate = Date()
        let entry = SimpleEntry(date: currentDate, isRecording: false)
        entries.append(entry)
        let timeline = Timeline(entries: entries, policy: .never)
        completion(timeline)
    }
}

struct SimpleEntry: TimelineEntry {
    let date: Date
    let isRecording: Bool
}

struct QuickRecordWidgetEntryView: View {
    var entry: Provider.Entry

    var body: some View {
        Link(destination: URL(string: "voicediary://quickrecord")!) {
            VStack {
                Image(systemName: "mic.fill")
                    .font(.system(size: 30))
                    .foregroundColor(.red)
                Text("快速录音")
                    .font(.caption)
            }
        }
    }
}

@main
struct QuickRecordWidget: Widget {
    let kind: String = "QuickRecordWidget"

    var body: some WidgetConfiguration {
        StaticConfiguration(kind: kind, provider: Provider()) { entry in
            QuickRecordWidgetEntryView(entry: entry)
        }
        .configurationDisplayName("快速录音")
        .description("点击立即开始录音")
        .supportedFamilies([.systemSmall])
    }
}
