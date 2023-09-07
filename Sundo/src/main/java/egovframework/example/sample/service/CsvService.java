package egovframework.example.sample.service;

public interface CsvService {
	
	public int insertCsv(CsvVO vo);
	public int insertLine(CsvVO vo);
	public int deleteLine();
}
