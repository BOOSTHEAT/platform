using System.Linq;
using System.Text;
using System.IO;

namespace ImpliciX.ToQml;

public interface ICopyrightManager
{
    void AddCopyright(Stream stream, string filename);
    string AddCopyright(string content, string filename);
}

public class CopyrightManager : ICopyrightManager
{
    private readonly string _applicationName;

    private readonly int _year;

    private static readonly string[] TargetExtensions = { ".cpp", ".qml", ".js" };

    public CopyrightManager(string applicationName, int year)
    {
        _applicationName = applicationName;
        _year = year;
    }

    public const string LicenseTemplate = @"/* Graphical User Interface for {0}
 Copyright (C) {1} BOOSTHEAT

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.
 
 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU General Public License for more details.
 
 You should have received a copy of the GNU General Public License
 along with this program.  If not, see <https://www.gnu.org/licenses/>.*/

";

    private string _copyrightNotice => string.Format(LicenseTemplate, _applicationName, _year);

    public void AddCopyright(Stream stream, string filename)
    {
        if (!TargetExtensions.Contains(Path.GetExtension(filename)))
            return;

        var copyrightNoticeBytes = Encoding.UTF8.GetBytes(_copyrightNotice);
        stream.Write(copyrightNoticeBytes, 0, copyrightNoticeBytes.Length);
    }

    public string AddCopyright(string content, string filename)
    {
        if (!TargetExtensions.Contains(Path.GetExtension(filename)))
            return content;

        content = _copyrightNotice + content;
        return content;
    }
}