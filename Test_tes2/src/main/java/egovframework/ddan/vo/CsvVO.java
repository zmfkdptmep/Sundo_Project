package egovframework.ddan.vo;

import javax.xml.bind.annotation.XmlRootElement;

import lombok.Data;

@XmlRootElement
@Data
public class CsvVO {
	
	private String lon;
    private String lat;
    private String time;
    private String car_num;
    private int noise;
    private int rpm;
    private String date;
  
}
