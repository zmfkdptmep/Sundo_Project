package egovframework.example.sample.service.impl;

import javax.annotation.Resource;

import org.egovframe.rte.fdl.cmmn.EgovAbstractServiceImpl;
import org.springframework.stereotype.Service;

import egovframework.example.sample.service.CsvService;
import egovframework.example.sample.service.CsvVO;

@Service("csvService")
public class CsvServiceImpl extends EgovAbstractServiceImpl implements CsvService{
	
	
	@Resource(name = "csvMapper")
	private CsvMapper mapper;
	
	@Override
	public int insertCsv(CsvVO vo) {
		return mapper.insertCsv(vo);
	}
	
	@Override
	public int insertLine(CsvVO vo) {
		return mapper.insertLine(vo);
	}

	@Override
	public int deleteLine() {
		return mapper.deleteLine();
	}

}
