package egovframework.ddan.controller;

import java.io.BufferedWriter;
import java.io.File;
import java.io.FileWriter;
import java.net.URL;
import java.text.SimpleDateFormat;
import java.util.Date;

import javax.xml.parsers.DocumentBuilder;
import javax.xml.parsers.DocumentBuilderFactory;

import org.w3c.dom.Document;
import org.w3c.dom.Element;
import org.w3c.dom.NodeList;

public class GpxParser {

    public static void main(String[] args) {
        try {
            // GPX file path
            String filePath = "C:\\Users\\user\\Desktop\\중요자료\\gpxFile\\GOTOES_6599877484456544.gpx";

            // Create a File object from the file path
            File file = new File(filePath);

            // Get a URL from the File object
            URL url = file.toURI().toURL();

            // XML parser settings
            DocumentBuilderFactory factory = DocumentBuilderFactory.newInstance();
            DocumentBuilder builder = factory.newDocumentBuilder();

            // Parse the GPX file and read it as a Document object
            Document document = builder.parse(url.toString());

            // Root element of GPX file
            Element root = document.getDocumentElement();

            // Get all "trkpt" elements
            NodeList trkptList = root.getElementsByTagName("trkpt");

            // Create a FileWriter and BufferedWriter to write to a CSV file
            FileWriter csvFileWriter = new FileWriter("output3.csv");
            BufferedWriter bufferedWriter = new BufferedWriter(csvFileWriter);

            // Write the CSV header
            bufferedWriter.write("Latitude,Longitude,Date,Time,Elevation");
            bufferedWriter.newLine();

            SimpleDateFormat inputDateFormat = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss'Z'");
            SimpleDateFormat outputDateFormatDate = new SimpleDateFormat("yyyy-MM-dd");
            SimpleDateFormat outputDateFormatTime = new SimpleDateFormat("HH:mm:ss");
            
            for (int i = 0; i < trkptList.getLength(); i++) {
                Element trkpt = (Element) trkptList.item(i);

                // Extract longitude and latitude
                String lon = trkpt.getAttribute("lon");
                String lat = trkpt.getAttribute("lat");

                // Extract time information
                Element timeElement = (Element) trkpt.getElementsByTagName("time").item(0);
                String timeStr = timeElement.getTextContent();

                // Parse the time and format it
                Date time = inputDateFormat.parse(timeStr);
                String formattedDate = outputDateFormatDate.format(time);
                String formattedTime = outputDateFormatTime.format(time);

                // Extract altitude information (ele element)
                Element eleElement = (Element) trkpt.getElementsByTagName("ele").item(0);
                String elevation = eleElement != null ? eleElement.getTextContent() : "N/A";

                // Write the data to the CSV file
                bufferedWriter.write(lat + "," + lon + "," + formattedDate + "," + formattedTime + "," + elevation);
                bufferedWriter.newLine();
            }

            // Close the BufferedWriter and FileWriter
            bufferedWriter.close();
            csvFileWriter.close();

            System.out.println("CSV file 'output.csv' has been created successfully.");

        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
