package egovframework.example.sample.web;

import java.io.BufferedWriter;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileReader;
import java.io.FileWriter;
import java.io.IOException;
import java.io.InputStreamReader;
import java.net.URL;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ScheduledExecutorService;

import javax.annotation.Resource;
import javax.xml.bind.annotation.XmlAccessorType;
import javax.xml.parsers.DocumentBuilder;
import javax.xml.parsers.DocumentBuilderFactory;
import javax.xml.parsers.ParserConfigurationException;

import org.springframework.context.annotation.Configuration;
import org.springframework.scheduling.annotation.EnableScheduling;
import org.springframework.stereotype.Component;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.ResponseBody;
import org.springframework.web.multipart.MultipartFile;
import org.w3c.dom.Document;
import org.w3c.dom.Element;
import org.w3c.dom.NodeList;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.google.gson.Gson;
import com.opencsv.CSVReader;
import com.opencsv.exceptions.CsvException;

import egovframework.example.sample.service.CarService;
import egovframework.example.sample.service.CarVO;
import egovframework.example.sample.service.CleanVO;
import egovframework.example.sample.service.CoopService;
import egovframework.example.sample.service.CsvService;
import egovframework.example.sample.service.CsvVO;
import egovframework.example.sample.service.PointService;
import egovframework.example.sample.service.PointVO;
import egovframework.example.sample.service.TempService;


@Configuration
@EnableScheduling
@Component
@Controller
public class CoopController {
	
	@Resource(name = "coopService")
	private CoopService service;
	
	@Resource(name = "tempService")
	private TempService service_temp;
	
	@Resource(name = "carService")
	private CarService service_car;
	
	@Resource(name = "pointService")
	private PointService service_point;
	
	@Resource(name = "csvService")
	private CsvService service_csv;
	
	private ScheduledExecutorService scheduler;
	
	@GetMapping("/main")
	public void main(Model model) throws JsonProcessingException {
		
		List<CarVO> list = service_car.getCarList();
		
		ObjectMapper objectMapper = new ObjectMapper();
		String carListJson = objectMapper.writeValueAsString(list);
		
		
		model.addAttribute("carList", carListJson);
		
		
	}
	
	@GetMapping("/main2")
	public void main2(Model model) {
		
	}
	
    @PostMapping("/date")
    public @ResponseBody String handleDateSubmission(@RequestBody String data) {
    	String result = "";
    	CleanVO res = null;
    	try {
    		
    		System.out.println(data);
    		Gson gson = new Gson();
    		CleanVO clean = gson.fromJson(data, CleanVO.class);
    		res = service_car.getClean(clean);
    		
    		if(res!=null) {
    			
    			result = "{ \"ratio\" : \""+res.getRatio()+"\", \"time\" : \""+res.getTime()+"\" , \"date\" : \""+clean.getDate()+"\" , \"lon\" : \""+res.getLon()+"\" , \"lat\" : \""+res.getLat()+"\" }";
    		} else {
    			
    			result = "{ \"res\" : \"no data\" }";
    		}
    		
    		
    	} catch (Exception e) {
    		e.printStackTrace();
    	}
    	System.out.println("res : "+ res);
    	System.out.println("result : "+ result);
    	
    	return result;
    }
    
	
	
	@PostMapping(value = "/android", consumes = "application/json")
	public void receiveLocation(@RequestBody String locationData) {
		
		
		 System.out.println(locationData);
		 
		 Gson gson = new Gson();
	     PointVO vo = gson.fromJson(locationData, PointVO.class);
	     
	     System.out.println("vo : "+ vo);
	     
	     
	     try {
	    	 
	    	 int res = service_point.insertPoint(vo);
	    	 
	    	 if(res>0) {
	    		 
	    		 System.out.println("좌표 DB 성공");
	    		 String data = "{ \"car_num\" : \"114하6585\" , \"date\" : \"2023-08-31\" }";
	    		 handleDateSubmission(data);
	    		 
	    		 
	    	 } else {
	    		 
	    		 System.err.println("좌표 DB 실패!!");
	    	 }
	     } catch (Exception e) {
	    	 e.printStackTrace();
	     }
		
	}
	
	@PostMapping("/upload")
	@ResponseBody
	public String uploadCsv(@RequestParam("file") MultipartFile[] files) {
	    String res = "";

	    List<Map<String, String>> mergedRecords = new ArrayList<>();

	    try {
	        for (MultipartFile file : files) {
	            List<Map<String, String>> records = parseCsv(file);
	            if (mergedRecords.isEmpty()) {
	                mergedRecords.addAll(records);
	            } else {
	                // 병합 로직
	                for (int i = 0; i < Math.min(mergedRecords.size(), records.size()); i++) {
	                    Map<String, String> existingRecord = mergedRecords.get(i);
	                    Map<String, String> newRecord = records.get(i);

	                    for (String key : newRecord.keySet()) {
	                        existingRecord.putIfAbsent(key, newRecord.get(key));
	                    }
	                }
	            }
	        }

	        Gson gson = new Gson();
	        // 결과 출력
	        for (Map<String, String> record : mergedRecords) {
	            System.out.println(record.toString());
	            
	            String str = record.toString();
	            
	            str = str.substring(1, str.length() - 1);  // 중괄호 제거
	            String[] keyValuePairs = str.split(",");  // 쉼표를 기준으로 분리

	            CsvVO vo = new CsvVO();
	            for(String pair : keyValuePairs) {
	                String[] entry = pair.split("=");  // 등호를 기준으로 분리
	                String key = entry[0].trim();
	                String value = entry[1].trim();

	                switch(key) {
	                    case "date":
	                        vo.setDate(value);
	                        break;
	                        // 현재 나의 csv 파일은 lat 과 lon이 바뀌어 있는 상태라서 이렇게 함
	                    case "latitude":
	                        vo.setLongitude(value);
	                        break;
	                    case "noise" :
	                    	vo.setNoise(Integer.parseInt(value));
	                    	break;
	                    case "car_num" :
	                    	vo.setCar_num(value);
	                    	break;
	                    case "time" :
	                    	vo.setTime(value);
	                    	break;
	                    case "rpm" :
	                    	vo.setRpm(Integer.parseInt(value));
	                    	break;
	                    case "longitude" :
	                    	vo.setLatitude(value);
	                    	break;
	                   
	                }
	                
	                
	            }
	            
	            System.out.println("vo : "+ vo);
	            
	            int result = service_csv.insertCsv(vo);
	            
	            if(result>0) {   	
	         
	            	
	            	int result2 = service_csv.insertLine(vo);
	            	
	            	if(result2> 0) {
	            		
	            		service_csv.deleteLine();
	            		
	            		System.err.println("CSV Line DB입력 성공~");
	            		
	            	} else {
	            		System.err.println("CSV Line DB입력 실패........");
	            		
	            	}
	            	
	            	System.err.println("CSV DB입력 성공~");
	            	
	            } else {
	            	System.err.println("CSV DB입력 실패..........");
	            }
	            
	        }
	        
	        
	        
	        
	        res = "success";

	    } catch (Exception e) {
	        e.printStackTrace();
	        res = "failed";
	    }
	    return res;
	}

	private List<Map<String, String>> parseCsv(MultipartFile file) throws IOException, CsvException {
	    List<Map<String, String>> records = new ArrayList<>();
	    try (CSVReader reader = new CSVReader(new InputStreamReader(file.getInputStream(), "EUC-KR"))) {
	        List<String[]> lines = reader.readAll();

	        // 첫 번째 라인에서 헤더 정보 가져오기
	        String[] headers = lines.get(0);
	        
	        // 레코드 읽기
	        for (int i = 1; i < lines.size(); i++) {
	            String[] line = lines.get(i);
	            Map<String, String> record = new HashMap<>();
	            for (int j = 0; j < Math.min(headers.length, line.length); j++) {
	                record.put(headers[j], line[j]);
	            }
	            records.add(record);
	        }
	    }
	    return records;
	}
	
	
	
	public static void gpxReader() throws ParserConfigurationException {
		
		try {
            // GPX file path
            String filePath = "D:\\filefile\\GOTOES_6065616026804906.gpx";

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
            FileWriter csvFileWriter = new FileWriter("D:\\filefile\\output.csv");
            BufferedWriter bufferedWriter = new BufferedWriter(csvFileWriter);

            // Write the CSV header
            bufferedWriter.write("Latitude,Longitude,Time,Elevation");
            bufferedWriter.newLine();

            for (int i = 0; i < trkptList.getLength(); i++) {
                Element trkpt = (Element) trkptList.item(i);

                // Extract longitude and latitude
                String lon = trkpt.getAttribute("lon");
                String lat = trkpt.getAttribute("lat");

                // Extract time information
                Element timeElement = (Element) trkpt.getElementsByTagName("time").item(0);
                String time = timeElement.getTextContent();

                // Extract altitude information (ele element)
                Element eleElement = (Element) trkpt.getElementsByTagName("ele").item(0);
                String elevation = eleElement != null ? eleElement.getTextContent() : "N/A";

                // Write the data to the CSV file
                bufferedWriter.write(lat + "," + lon + "," + time + "," + elevation);
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

	
	
	
	public static void main(String[] args) throws ParserConfigurationException {
		
		gpxReader();


		
	}
}
