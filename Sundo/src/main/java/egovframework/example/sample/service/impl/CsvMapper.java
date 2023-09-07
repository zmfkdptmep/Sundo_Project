package egovframework.example.sample.service.impl;

import org.egovframe.rte.psl.dataaccess.mapper.Mapper;

import egovframework.example.sample.service.CsvVO;

@Mapper("csvMapper")
public interface CsvMapper {
	
	public int insertCsv(CsvVO vo);
	
	public int insertLine(CsvVO vo);
	
	public int deleteLine();

}
