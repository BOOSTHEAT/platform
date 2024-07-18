#include <QApplication>
#include <QQmlApplicationEngine>
#include <QLibraryInfo>
#include "main.h"

int main(int argc, char *argv[])
{
    qputenv("QT_IM_MODULE", QByteArray("qtvirtualkeyboard"));
    qputenv("QT_VIRTUALKEYBOARD_LAYOUT_PATH", QByteArray(IMPLICIX_VIRTUALKEYBOARD_LAYOUT_PATH));
    
    QCoreApplication::setAttribute(Qt::AA_EnableHighDpiScaling);
    QApplication app(argc, argv);

    QQmlApplicationEngine engine;
    engine.addImportPath("qrc:/");
    engine.addImportPath("qrc:/Keyboards");
    engine.addImportPath("qrc:/LocalKeyboards");
    const QUrl url(QStringLiteral("qrc:/main.qml"));
    QObject::connect(&engine, &QQmlApplicationEngine::objectCreated,
                     &app, [url](QObject *obj, const QUrl &objUrl) {
        if (!obj && url == objUrl)
            QCoreApplication::exit(-1);
    }, Qt::QueuedConnection);
    engine.load(url);

    return app.exec();
}
