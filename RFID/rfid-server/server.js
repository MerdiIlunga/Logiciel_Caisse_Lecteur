const SerialPort = require('serialport');
const { ReadlineParser } = require('@serialport/parser-readline');
const sql = require('mssql');

// Port série (ajuste COMx si besoin)
const port = new SerialPort.SerialPort({
  path: 'COM3',
  baudRate: 9600
});

const parser = port.pipe(new ReadlineParser({ delimiter: '\n' }));

// Configuration SQL Server
const sqlConfig = {
  user: 'jc',
  password: 'Patate123',
  server: 'localhost',
  database: 'PointOfSaleDB',
  options: {
    trustServerCertificate: true
  }
};

// Connexion SQL
sql.connect(sqlConfig)
  .then(() => console.log('Connecté à SQL Server'))
  .catch(err => console.error('Erreur SQL:', err));

// Traitement des données série
parser.on('data', async (data) => {
    console.log('Données reçues:', data);

    try {
        const rfidData = JSON.parse(data);
        if (!rfidData.card_id || !rfidData.role) return;

        const access = rfidData.role === "admin" ? "accepté" : "refusé";

        await sql.query`
        EXEC sp_AddRFIDScan 
            @CardID = ${rfidData.card_id}, 
            @Role = ${rfidData.role},
            @Access = ${access}
        `;

        console.log(`✅ Accès ${access} enregistré pour ${rfidData.card_id}`);
    } catch (err) {
        console.error('Erreur de traitement:', err.message);
    }
});
  

// Gestion erreurs
port.on('error', (err) => {
  console.error('Erreur port série:', err);
});
