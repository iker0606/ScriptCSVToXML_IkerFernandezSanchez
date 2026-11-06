using System;
using System.IO;
using System.Xml;

// Para que el programa coja la ruta de los archivos desde la carpeta del proyecto y no desde la carpeta bin/debug/net10
string carpetaProyecto = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
if (File.Exists(Path.Combine(carpetaProyecto, "Practica2_2.csproj"))) { // Solo cambia si encuentra el .csproj
    Directory.SetCurrentDirectory(carpetaProyecto);
}

string rutaOrigen = "NetflixOriginals.csv"; // Sera CSV o XML
string rutaDestino = ""; // Sera el que toque

// Pide el fichero de origen 
void origen() {
    Console.Write("Ruta del fichero de origen (.csv o .xml): ");
    string ruta = Console.ReadLine();
    if (ruta != "") { // Si no esta vacia se cambia la ruta de origen
        while (!File.Exists(ruta)) {
            Console.WriteLine("No existe. Ruta del fichero de origen: ");
            ruta = Console.ReadLine();
        }
        rutaOrigen = ruta;
    } else {
        Console.WriteLine("Ruta de origen no modificada.");
    }

}

// Pide la carpeta de destino (si se deja vacia se usa la carpeta del proyecto)
void destino() {
    Console.Write("Ruta de la carpeta de destino: ");
    rutaDestino = Console.ReadLine();
    if (rutaDestino == "") {
        Console.WriteLine("Carpeta del proyecto elegida como ruta de destino.");
    }
}

string nombre() { // Pide el nombre del fichero de destino hasta que no este vacio
    Console.Write("Nombre del fichero de destino (con la extensión): ");
    string nombre = Console.ReadLine();
    while (nombre == "") {
        Console.Write("Nombre no válido. Nombre del fichero de destino: ");
        nombre = Console.ReadLine();
    }
    return Path.Combine(rutaDestino, nombre); // Se añade el nombre a la ruta de destino
}

string limpiarComas(string linea) { // Quita las comas de los años
    string limpia = "";
    bool dentro = false;
    foreach (char c in linea) { // Recorre todos los caracteres de la linea
        if (c == '"') { // Si encuentra una comilla cambia el estado de dentro
            dentro = !dentro;
        } else if (c == ',' && dentro) { // Si encuentra una coma dentro de comillas la cambia por º
            limpia += 'º'; 
        } else { // Si no es una coma dentro de comillas la añade tal cual
            limpia += c;
        }
    }
    return limpia; // Devuelve la linea sin comas dentro
}

// Devuelve las lineas del CSV limpias
string[] CSVlimpio() {
    string[] lineas = File.ReadAllLines(rutaOrigen);
    for (int i = 0; i < lineas.Length; i++) {
        lineas[i] = limpiarComas(lineas[i]); // Cada linea se limpia y se sobrecribe
    }
    return lineas;
}

// Devuelve las líneas del XML en formato CSV
string[] XMLlimpio() {
    XmlDocument doc = new XmlDocument();
    doc.Load(rutaOrigen); // Carga el XML 
    XmlNodeList peliculas = doc.SelectNodes("Peliculas/Pelicula"); // Carga la lista de los campos de cada peli

    string[] lineas = new string[peliculas.Count + 1]; // +1 porque la primera linea son los campos
    lineas[0] = "Title,Genre,Premiere,Runtime,IMDB Score,Language";

    for (int i = 0; i < peliculas.Count; i++) { // Recorre todas las pelis
        string linea = "";
        for (int j = 0; j < 6; j++) { // Recorre los campos de la peli i
            if (j > 0) { // A partir del primero se pone coma para separar los campos
                linea += ",";
            }
            // Se añaden los valores (devolviendoles la coma) de los campos de cada peli 
            linea += peliculas[i].ChildNodes[j].InnerText.Replace(',', 'º');
        }
        lineas[i + 1] = linea; // Se añade la linea a la lista de lineas
    }
    return lineas;
}

// Pregunta el filtro, si no es valido da null
string queFiltro(int opcion) {
    if (opcion == 1) { // La primera opcion no tiene filtro
        return "";
    }

    if (opcion == 3) { // Filtro por año
        Console.Write("Año: ");
        string anio = Console.ReadLine();
        int numero = 0;
        if (int.TryParse(anio, out numero)) {
            return anio;
        }
        return null;
    }

    Console.Write("Escribe cuál quieres: "); // Los que faltan son por genero o lenguaje
    string filtro = Console.ReadLine().ToLower();
    if (filtro == "") {
        return null;
    }
    return filtro;
}

// Dice si una línea hay que exportarla o no
bool valida(string linea, int opcion, string filtro) {
    if (string.IsNullOrWhiteSpace(linea)) { // Ignora las lineas vacias
        return false;
    }

    string[] valores = linea.Split(','); // Array de los valores spliteados

    if (opcion == 1) { // No hay filtro asi que todas valen
        return true;
    }

    if (opcion == 2) { // Genero
        return valores[1].ToLower().Contains(filtro); // valores[1] es el genero
    }

    if (opcion == 3) { // Año
        string[] partes = valores[2].Split(' '); // Separa dia mes y año
        int anioPeli = 0;
        int anioFiltro = 0;
        int.TryParse(partes[2], out anioPeli); // El año es el ultimo indice de las 3 posiciones
        int.TryParse(filtro, out anioFiltro); // Mete el año que se da en la variable
        return anioPeli >= anioFiltro;
    }

    return valores[5].ToLower().Contains(filtro); // La que queda es por lenguaje que es valores[5]
}

// Recorre todas las lineas y dice si hay minimo una que sirva
bool hayMinUna(string[] lineas, int opcion, string filtro) {
    for (int i = 1; i < lineas.Length; i++) {
        if (valida(lineas[i], opcion, filtro)) {
            return true;
        }
    }
    return false;
}

void hacerXML(string[] lineas, int opcion, string filtro, string rDestino) { // Crea el XML a partir del CSV
    string[] campos = lineas[0].Split(','); // Array de los campos

    XmlWriterSettings ajustes = new XmlWriterSettings();
    ajustes.Indent = true; // Para que se vea bien el xml

    using XmlWriter xml = XmlWriter.Create(rDestino, ajustes); // Crea el xml con los ajustes
    xml.WriteStartDocument(); 
    xml.WriteStartElement("Peliculas");

    for (int i = 1; i < lineas.Length; i++) { // Recorre todas las lineas despues de la primera (campos)
        if (valida(lineas[i], opcion, filtro)) { // Si la linea es valida se escribe
            string[] valores = lineas[i].Split(',');
            xml.WriteStartElement("Pelicula");
            for (int j = 0; j < campos.Length; j++) { // Si es valida recorre todos los campos y escribe sus valores
                // No puede haber espacios en etiquetas y se recuperan las comas que se habian cambiado por º
                xml.WriteElementString(campos[j].Replace(" ", ""), valores[j].Replace('º', ','));
            }
            xml.WriteEndElement();
        }
    }

    xml.WriteEndElement();
    xml.WriteEndDocument();
    xml.Close(); // Acaba el documento XML
}

void hacerCSV(string[] lineas, int opcion, string filtro, string rDestino) { // Crea el CSV a partir del XML
    using StreamWriter csv = new StreamWriter(rDestino);  // Crea el CSV
    csv.WriteLine(lineas[0]); // Escribe la primera linea que son los campos y ya estan en formato CSV 

    for (int i = 1; i < lineas.Length; i++) { // Recorre todas las lineas despues de la primera (campos)
        if (valida(lineas[i], opcion, filtro)) { // Si la linea es valida se escribe
            string[] valores = lineas[i].Split(','); // Separa los valores de la linea
            string linea = ""; // Se crea la linea que se va a escribir en el CSV
            for (int j = 0; j < valores.Length; j++) { // Recorre todos los valores de la linea
                string valor = valores[j].Replace('º', ','); // Se recuperan las comas
                if (valor.Contains(',')) { // Si el valor contiene comas se pone entre comillas
                    valor = "\"" + valor + "\""; // La contrabarra es para que tome " como caracter y no como fin de cadena
                }
                if (j > 0) {
                    linea += ","; // A partir del primero se pone coma para separar los valores
                }
                linea += valor; // Se añade el valor a la linea
            }
            csv.WriteLine(linea); // Se escribe la linea en el CSV
        }
    }

    csv.Close(); // Acaba el documento CSV
}

void cambiar(int opcion) { // Es la primera funcion que se llama cuando se elige una opcion del menu
    bool esCSV = rutaOrigen.ToLower().EndsWith(".csv"); // Dice si el fichero de origen es CSV o XML

    string[] lineas; // Se crea un array de lineas que sera el que toque dependiendo del fichero de origen

    if (esCSV) { // Primero se limpia el CSV, si es XML al limpiarlo se deja en formato CSV
        lineas = CSVlimpio();
    } else {
        lineas = XMLlimpio();
        
    }

    string filtro = queFiltro(opcion); // Pregunta el filtro dependiendo de la opcion elegida

    // Si el filtro no es valido o no hay ninguna peli
    if (filtro == null || !hayMinUna(lineas, opcion, filtro)) {
        Console.WriteLine("Valor no válido, volviendo al menú...");
        return;
    }

    try {
        if (rutaDestino != "") {
            Directory.CreateDirectory(rutaDestino); // Crea la carpeta de destino si no existe
        }

        if (esCSV) { // Se crea el documento que toque
            hacerXML(lineas, opcion, filtro, nombre());
        } else {
            hacerCSV(lineas, opcion, filtro, nombre());
        }
        Console.WriteLine("Hecho.");
    } catch (Exception e) {
        Console.WriteLine("No se pudo crear el fichero: " + e.Message);
    }
}

origen(); 
destino();

int opcion = 0;
while (opcion != 7) { // Menu principal
    Console.WriteLine();
    Console.WriteLine("1. Exportar todo el documento");
    Console.WriteLine("2. Exportar por género (en inglés)");
    Console.WriteLine("3. Exportar por año");
    Console.WriteLine("4. Exportar por lenguaje (en inglés)");
    Console.WriteLine("5. Cambiar fichero de origen"); 
    Console.WriteLine("6. Cambiar la carpeta de destino"); 
    Console.WriteLine("7. Cerrar la aplicación");
    Console.Write("Opción: ");

    int.TryParse(Console.ReadLine(), out opcion);

    if (opcion >= 1 && opcion <= 4) {
        cambiar(opcion);
    } else if (opcion == 5) { 
        origen();
    } else if (opcion == 6) {
        destino();
    } else if (opcion == 7) {
        Console.WriteLine("Agur.");
    } else {
        Console.WriteLine("Opción no válida.");
    }
}