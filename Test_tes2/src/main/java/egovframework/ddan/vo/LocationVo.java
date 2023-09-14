package egovframework.ddan.vo;

import java.sql.Time;

import org.postgis.Geometry;

import com.fasterxml.jackson.annotation.JsonProperty;

import lombok.Data;

@Data
public class LocationVo {
	
	
	@JsonProperty("latitude")
	private String latitude;
	
	@JsonProperty("longitude")
	private String longitude;
	
	@JsonProperty("andoid_id")
	private String andoid_id;
	
	@JsonProperty("noise")
	private int noise;
	
	@JsonProperty("rpm")
	private int rpm; 
	
	@JsonProperty("car_num")
	private String car_num;
	
	private String date;
	private Time time;
	
}
